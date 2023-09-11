// Copyright (c) 2019-2022 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Models;

namespace AccelByte.Api
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class AccelBytePlatformSettingsEditor : EditorWindow
    {
        private static AccelBytePlatformSettingsEditor instance;
        public static AccelBytePlatformSettingsEditor Instance
        {
            get
            {
                return instance;
            }
        }
        private const string windowTitle = "AccelByte Configuration";
        private Texture2D accelByteLogo;
        private int temporaryEnvironmentSetting;
        private int temporaryPlatformSetting;
        private int temporaryPresenceBroadcastEventGameStateSetting;
        private string[] environmentList;
        private string[] platformList;
        private string[] presenceBroadcastEventGameStateList;
        private Rect logoRect;

        private AccelByteAnalyticsSettings analyticsSettings;
        private MultiOAuthConfigs originalClientOAuthConfigs;
        private MultiOAuthConfigs originalAnalyticsOAuthConfigs;
        private MultiConfigs originalSdkConfigs;
        private OAuthConfig editedAnalyticsOAuthConfig;
        private OAuthConfig editedClientOAuthConfig;
        private Config editedSdkConfig;
        private Vector2 scrollPos;
        private bool showAnalyticsConfigs;
        private bool showCacheConfigs;
        private bool showOtherConfigs;
        private bool showLogConfigs;
        private bool showServiceUrlConfigs;
        private bool showTURNconfigs;
        private bool showPresenceBroadcastEventConfig;
        private bool showPreDefinedEventConfig;
        private bool showClientAnalyticsEventConfig;
        private GUIStyle requiredTextFieldGUIStyle;
        private bool initialized;
        private bool generateServiceUrl = true;

        [MenuItem("AccelByte/Edit Settings")]
        public static void Edit()
        {
            // Get existing open window or if none, make a new one:
            if (instance != null)
            {
                instance.CloseFinal();
            }

            instance = EditorWindow.GetWindow<AccelBytePlatformSettingsEditor>(windowTitle, true, System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor.dll"));
            instance.Show();
        }

        private void Initialize()
        {
            if (!initialized)
            {
                requiredTextFieldGUIStyle = new GUIStyle();
                requiredTextFieldGUIStyle.normal.textColor = Color.yellow;

                accelByteLogo = Resources.Load<Texture2D>("ab-logo");

                platformList = new string[]
                {
                    PlatformType.Steam.ToString(),
                    PlatformType.Apple.ToString(),
                    PlatformType.iOS.ToString(),
                    PlatformType.Android.ToString(),
                    PlatformType.PS4.ToString(),
                    PlatformType.PS5.ToString(),
                    PlatformType.Live.ToString(),
                    PlatformType.Nintendo.ToString(),
                    "Default"
                };
                this.temporaryPlatformSetting = platformList.Length - 1;

                environmentList = new string[]
                {
                    "Development",
                    "Certification",
                    "Production",
                    "Default"
                };
                temporaryEnvironmentSetting = environmentList.Length - 1;

                presenceBroadcastEventGameStateList = new string[]
                {
                    AccelByte.Utils.JsonUtils.SerializeWithStringEnum(
                        PresenceBroadcastEventGameState.OutOfGameplay),
                    AccelByte.Utils.JsonUtils.SerializeWithStringEnum(
                        PresenceBroadcastEventGameState.InGameplay),
                    AccelByte.Utils.JsonUtils.SerializeWithStringEnum(
                        PresenceBroadcastEventGameState.Store),
                };
                temporaryPresenceBroadcastEventGameStateSetting = 0;

                logoRect = new Rect((this.position.width - 300) / 2, 10, 300, 86);

                analyticsSettings = new AccelByteAnalyticsSettings();

                initialized = true;
            }

            if(originalSdkConfigs == null)
            {
                originalSdkConfigs = AccelByteSettingsV2.LoadSDKConfigFile();
                if(originalSdkConfigs == null)
                {
                    originalSdkConfigs = new MultiConfigs();
                }
            }
            if (originalClientOAuthConfigs == null)
            {
                originalClientOAuthConfigs = AccelByteSettingsV2.LoadOAuthFile(GetPlatformName(platformList, temporaryPlatformSetting));
                if (originalClientOAuthConfigs == null)
                {
                    originalClientOAuthConfigs = new MultiOAuthConfigs();
                }
            }
            if (originalAnalyticsOAuthConfigs == null)
            {
                try
                {
                    originalAnalyticsOAuthConfigs = analyticsSettings.LoadOAuthFile(GetPlatformName(platformList, temporaryPlatformSetting), false);
                }
                catch (Exception)
                {

                }
                
                if (originalAnalyticsOAuthConfigs == null)
                {
                    originalAnalyticsOAuthConfigs = new MultiOAuthConfigs();
                }
            }

            if (editedSdkConfig == null)
            {
                var originalSdkConfig = AccelByteSettingsV2.GetSDKConfigByEnvironment(originalSdkConfigs, (SettingsEnvironment)temporaryEnvironmentSetting);
                editedSdkConfig = originalSdkConfig != null ? originalSdkConfig.ShallowCopy() : new Config();
            }
            if (editedClientOAuthConfig == null)
            {
                var originalClientOAuthConfig = AccelByteSettingsV2.GetOAuthByEnvironment(originalClientOAuthConfigs, (SettingsEnvironment)temporaryEnvironmentSetting);
                editedClientOAuthConfig = originalClientOAuthConfig != null ? originalClientOAuthConfig.ShallowCopy() : new OAuthConfig();
            }
            if (editedAnalyticsOAuthConfig == null)
            {
                var originalAnalyticsOAuthConfig = AccelByteSettingsV2.GetOAuthByEnvironment(originalAnalyticsOAuthConfigs, (SettingsEnvironment)temporaryEnvironmentSetting);
                editedAnalyticsOAuthConfig = originalAnalyticsOAuthConfig != null ? originalAnalyticsOAuthConfig.ShallowCopy() : new OAuthConfig();
            }
        }

        private void CloseFinal()
        {
            Close();
            instance = null;
        }

        private void OnGUI()
        {
#if UNITY_EDITOR
            Initialize();

            logoRect.x = (this.position.width - 300) / 2;
            GUI.DrawTexture(logoRect, accelByteLogo);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(100);

            if (EditorApplication.isPlaying)
            {
                CloseFinal();
                return;
            }

            {
                var originalSdkConfig = AccelByteSettingsV2.GetSDKConfigByEnvironment(originalSdkConfigs, (SettingsEnvironment)temporaryEnvironmentSetting);
                var originalClientOAuthConfig = AccelByteSettingsV2.GetOAuthByEnvironment(originalClientOAuthConfigs, (SettingsEnvironment)temporaryEnvironmentSetting);
                var originalAnalyticsOAuthConfig = AccelByteSettingsV2.GetOAuthByEnvironment(originalAnalyticsOAuthConfigs, (SettingsEnvironment)temporaryEnvironmentSetting);

                if (CompareOAuthConfig(editedClientOAuthConfig, originalClientOAuthConfig) && CompareConfig(editedSdkConfig, originalSdkConfig) && CompareOAuthConfig(editedAnalyticsOAuthConfig, originalAnalyticsOAuthConfig))
                {
                    EditorGUILayout.HelpBox("All configs has been saved!", MessageType.Info, true);
                }
                else
                {
                    EditorGUILayout.HelpBox("Unsaved changes", MessageType.Warning, true);
                }
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("SDK Version");
            EditorGUILayout.LabelField(AccelByteSettingsV2.AccelByteSDKVersion);
            EditorGUILayout.LabelField("");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Environment");
            EditorGUI.BeginChangeCheck();
            temporaryEnvironmentSetting = EditorGUILayout.Popup(temporaryEnvironmentSetting, environmentList);
            if (EditorGUI.EndChangeCheck())
            {
                var originalSdkConfig = AccelByteSettingsV2.GetSDKConfigByEnvironment(originalSdkConfigs, (SettingsEnvironment)temporaryEnvironmentSetting);
                var originalClientOAuthConfig = AccelByteSettingsV2.GetOAuthByEnvironment(originalClientOAuthConfigs, (SettingsEnvironment)temporaryEnvironmentSetting);
                var originalAnalyticsOAuthConfig = AccelByteSettingsV2.GetOAuthByEnvironment(originalAnalyticsOAuthConfigs, (SettingsEnvironment)temporaryEnvironmentSetting);

                editedSdkConfig = originalSdkConfig != null ? originalSdkConfig.ShallowCopy() : new Config();
                editedClientOAuthConfig = originalClientOAuthConfig != null ? originalClientOAuthConfig.ShallowCopy() : new OAuthConfig();
                editedAnalyticsOAuthConfig = originalAnalyticsOAuthConfig != null ? originalAnalyticsOAuthConfig.ShallowCopy() : new OAuthConfig();
            }
            EditorGUILayout.LabelField("");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Platform");
            EditorGUI.BeginChangeCheck();
            temporaryPlatformSetting = EditorGUILayout.Popup(temporaryPlatformSetting, platformList);
            if (EditorGUI.EndChangeCheck())
            {
                string targetPlatform = "";
                if (platformList[temporaryPlatformSetting] != "Default")
                {
                    targetPlatform = platformList[temporaryPlatformSetting];
                }
                originalClientOAuthConfigs = AccelByteSettingsV2.LoadOAuthFile(targetPlatform);
                try
                {
                    originalAnalyticsOAuthConfigs = analyticsSettings.LoadOAuthFile(targetPlatform, false);
                }
                catch (Exception)
                {
                    originalAnalyticsOAuthConfigs = null;
                }

                editedClientOAuthConfig = originalClientOAuthConfigs != null ? AccelByteSettingsV2.GetOAuthByEnvironment(originalClientOAuthConfigs, (SettingsEnvironment)temporaryEnvironmentSetting) : new OAuthConfig();
                editedAnalyticsOAuthConfig = originalClientOAuthConfigs != null ? AccelByteSettingsV2.GetOAuthByEnvironment(originalAnalyticsOAuthConfigs, (SettingsEnvironment)temporaryEnvironmentSetting) : new OAuthConfig();
            }
            EditorGUILayout.LabelField("");
            EditorGUILayout.EndHorizontal();

            CreateTextInput((newValue) => editedSdkConfig.BaseUrl = newValue, editedSdkConfig.BaseUrl, "Base Url", true);
            CreateTextInput((newValue) => editedSdkConfig.RedirectUri = newValue, editedSdkConfig.RedirectUri, "Redirect Uri", true);
            CreateTextInput((newValue) => editedSdkConfig.Namespace = newValue, editedSdkConfig.Namespace, "Namespace", true);
            CreateTextInput((newValue) => editedSdkConfig.PublisherNamespace = newValue, editedSdkConfig.PublisherNamespace, "Publisher Namespace", true);
            CreateTextInput((newValue) => editedClientOAuthConfig.ClientId = newValue, editedClientOAuthConfig.ClientId, "Client Id", true);
            CreateTextInput((newValue) => editedClientOAuthConfig.ClientSecret = newValue, editedClientOAuthConfig.ClientSecret, "Client Secret");
            CreateTextInput((newValue) => editedSdkConfig.AppId = newValue, editedSdkConfig.AppId, "App Id");

            showAnalyticsConfigs = EditorGUILayout.Foldout(showAnalyticsConfigs, "Analytics Configs");
            if (showAnalyticsConfigs)
            {
                CreateTextInput((newValue) => editedAnalyticsOAuthConfig.ClientId = newValue, editedAnalyticsOAuthConfig.ClientId, "Analytics Client Id");
                CreateTextInput((newValue) => editedAnalyticsOAuthConfig.ClientSecret = newValue, editedAnalyticsOAuthConfig.ClientSecret, "Analytics Client Secret");
            }

            showCacheConfigs = EditorGUILayout.Foldout(showCacheConfigs, "Cache Configs");
            if (showCacheConfigs)
            {
                Action<double> onCacheSizeChanged = (newValue) =>
                {
                    if(newValue > 0)
                    {
                        editedSdkConfig.MaximumCacheSize = Mathf.FloorToInt((float)newValue);
                    }
                };
                CreateNumberInput(onCacheSizeChanged, editedSdkConfig.MaximumCacheSize, "Cache Size");

                Action<double> onCacheLifeTimeChanged = (newValue) =>
                {
                    if (newValue > 0)
                    {
                        editedSdkConfig.MaximumCacheLifeTime = Mathf.FloorToInt((float)newValue);
                    }
                };
                CreateNumberInput(onCacheLifeTimeChanged, editedSdkConfig.MaximumCacheLifeTime, "Cache Life Time (Seconds)");
            }

            showOtherConfigs = EditorGUILayout.Foldout(showOtherConfigs, "Other Configs");
            if (showOtherConfigs)
            {
                CreateTextInput((newValue) => editedSdkConfig.CustomerName = newValue, editedSdkConfig.CustomerName, "Customer Name");
                CreateToggleInput((newValue) => editedSdkConfig.UsePlayerPrefs = newValue, editedSdkConfig.UsePlayerPrefs, "Use PlayerPrefs");
            }

            showLogConfigs = EditorGUILayout.Foldout(showLogConfigs, "Log Configs");
            if (showLogConfigs)
            {
                CreateToggleInput((newValue) => editedSdkConfig.EnableDebugLog = newValue, editedSdkConfig.EnableDebugLog, "Enable Debug Log");
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Log Type Filter");
                AccelByte.Core.AccelByteLogType currentLogFilter;
                if (!Enum.TryParse(editedSdkConfig.DebugLogFilter, out currentLogFilter))
                {
                    currentLogFilter = AccelByte.Core.AccelByteLogType.Verbose;
                }
                var newLogFilter = (AccelByte.Core.AccelByteLogType)EditorGUILayout.EnumPopup(currentLogFilter);
                editedSdkConfig.DebugLogFilter = newLogFilter.ToString();

                EditorGUILayout.LabelField("");
                EditorGUILayout.EndHorizontal();
            }

            showServiceUrlConfigs = EditorGUILayout.Foldout(showServiceUrlConfigs, "Service Url Configs");
            if(showServiceUrlConfigs)
            {
                CreateToggleInput((newValue) => generateServiceUrl = newValue, generateServiceUrl, "Auto Generate Service Url");
                CreateTextInput((newValue) => editedSdkConfig.IamServerUrl = newValue, editedSdkConfig.IamServerUrl, "IAM Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.PlatformServerUrl = newValue, editedSdkConfig.PlatformServerUrl, "Platform Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.BasicServerUrl = newValue, editedSdkConfig.BasicServerUrl, "Basic Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.LobbyServerUrl = newValue, editedSdkConfig.LobbyServerUrl, "Lobby Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.CloudStorageServerUrl = newValue, editedSdkConfig.CloudStorageServerUrl, "Cloud Storage Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.GameProfileServerUrl = newValue, editedSdkConfig.GameProfileServerUrl, "Game Profile Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.StatisticServerUrl = newValue, editedSdkConfig.StatisticServerUrl, "Statistic Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.AchievementServerUrl = newValue, editedSdkConfig.AchievementServerUrl, "Achievement Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.CloudSaveServerUrl = newValue, editedSdkConfig.CloudSaveServerUrl, "CloudSave Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.AgreementServerUrl = newValue, editedSdkConfig.AgreementServerUrl, "Agreement Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.LeaderboardServerUrl = newValue, editedSdkConfig.LeaderboardServerUrl, "Leaderboard Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.GameTelemetryServerUrl = newValue, editedSdkConfig.GameTelemetryServerUrl, "Game Telemetry Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.GroupServerUrl = newValue, editedSdkConfig.GroupServerUrl, "Group Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.SeasonPassServerUrl = newValue, editedSdkConfig.SeasonPassServerUrl, "Season Pass Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.SessionBrowserServerUrl = newValue, editedSdkConfig.SessionBrowserServerUrl, "Session BrowserServer Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.SessionServerUrl = newValue, editedSdkConfig.SessionServerUrl, "Session Server Url", false, generateServiceUrl);
                CreateTextInput((newValue) => editedSdkConfig.MatchmakingV2ServerUrl = newValue, editedSdkConfig.MatchmakingV2ServerUrl, "MatchmakingV2 Server Url", false, generateServiceUrl);
            }

            showTURNconfigs = EditorGUILayout.Foldout(showTURNconfigs, "TURN Configs");
            if (showTURNconfigs)
            {
                CreateToggleInput((newValue) => editedSdkConfig.UseTurnManager = newValue, editedSdkConfig.UseTurnManager, "Use TURN Manager");
                CreateToggleInput((newValue) => editedSdkConfig.EnableAuthHandshake = newValue, editedSdkConfig.EnableAuthHandshake, "Use Secure Handshaking");
                CreateTextInput((newValue) => editedSdkConfig.TurnServerHost = newValue, editedSdkConfig.TurnServerHost, "TURN Server Host");
                CreateTextInput((newValue) => editedSdkConfig.TurnServerPort = newValue, editedSdkConfig.TurnServerPort, "TURN Server Port");
                CreateTextInput((newValue) => editedSdkConfig.TurnManagerServerUrl = newValue, editedSdkConfig.TurnManagerServerUrl, "TURN Manager Server Url");
                CreateTextInput((newValue) => editedSdkConfig.TurnServerUsername = newValue, editedSdkConfig.TurnServerUsername, "TURN Server Username");
                CreateTextInput((newValue) => editedSdkConfig.TurnServerSecret = newValue, editedSdkConfig.TurnServerSecret, "TURN Server Secret");
                CreateTextInput((newValue) => editedSdkConfig.TurnServerPassword = newValue, editedSdkConfig.TurnServerPassword, "TURN Server Password");
                CreateNumberInput((newvalue) => editedSdkConfig.PeerMonitorIntervalMs = (int)newvalue, editedSdkConfig.PeerMonitorIntervalMs, "Peer Monitor Interval in Milliseconds");
                CreateNumberInput((newvalue) => editedSdkConfig.PeerMonitorTimeoutMs = (int)newvalue, editedSdkConfig.PeerMonitorTimeoutMs, "Peer Monitor Timeout in Milliseconds");
                CreateNumberInput((newvalue) => editedSdkConfig.HostCheckTimeoutInSeconds = (int)newvalue, editedSdkConfig.HostCheckTimeoutInSeconds, "Host Check Timeout in Seconds");
            }

            showPresenceBroadcastEventConfig = EditorGUILayout.Foldout(showPresenceBroadcastEventConfig, "Presence Broadcast Event Configs");
            if (showPresenceBroadcastEventConfig)
            {
                CreateToggleInput((newValue) => editedSdkConfig.EnablePresenceBroadcastEvent = newValue, editedSdkConfig.EnablePresenceBroadcastEvent, "Enable Presence Broadcast Event");
                CreateNumberInput((newValue) => editedSdkConfig.PresenceBroadcastEventInterval = (int)newValue, editedSdkConfig.PresenceBroadcastEventInterval, "Set Interval In Seconds");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Game State");
                EditorGUI.BeginChangeCheck();
                temporaryPresenceBroadcastEventGameStateSetting = EditorGUILayout.Popup(temporaryPresenceBroadcastEventGameStateSetting, presenceBroadcastEventGameStateList);
                if (EditorGUI.EndChangeCheck())
                {
                    int gameState = 0;
                    if (presenceBroadcastEventGameStateList[temporaryPresenceBroadcastEventGameStateSetting] !=
                        AccelByte.Utils.JsonUtils.SerializeWithStringEnum(
                        PresenceBroadcastEventGameState.OutOfGameplay))
                    {
                        gameState = temporaryPresenceBroadcastEventGameStateSetting;
                    }
                    editedSdkConfig.PresenceBroadcastEventGameState = gameState;
                }
                EditorGUILayout.LabelField("");
                EditorGUILayout.EndHorizontal();

                CreateTextInput((newValue) => editedSdkConfig.PresenceBroadcastEventGameStateDescription = newValue, editedSdkConfig.PresenceBroadcastEventGameStateDescription, "Set Game State description");
            }

            showPreDefinedEventConfig = EditorGUILayout.Foldout(showPreDefinedEventConfig, "Pre-Defined Event Configs");
            if (showPreDefinedEventConfig)
            {
                CreateToggleInput((newValue) => editedSdkConfig.EnablePreDefinedEvent = newValue, editedSdkConfig.EnablePreDefinedEvent, "Enable Pre-Defined Game Event");
            }
            
            showClientAnalyticsEventConfig = EditorGUILayout.Foldout(showClientAnalyticsEventConfig, "Client Analytics Event Configs");
            if (showClientAnalyticsEventConfig)
            {
                CreateToggleInput((newValue) => editedSdkConfig.EnableClientAnalyticsEvent = newValue, editedSdkConfig.EnableClientAnalyticsEvent, "Enable Client Analytics Event");
                CreateNumberInput((newValue) => editedSdkConfig.ClientAnalyticsEventInterval = newValue, editedSdkConfig.ClientAnalyticsEventInterval, "Set Interval In Seconds");

                const float minimalInterval = Core.ClientAnalyticsEventScheduler.ClientAnalyticsMiniumAllowedIntervalInlMs / 1000f;
                if (editedSdkConfig.ClientAnalyticsEventInterval < minimalInterval)
                {
                    editedSdkConfig.ClientAnalyticsEventInterval = minimalInterval;
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (GUILayout.Button("Save"))
            {
                editedClientOAuthConfig.Expand();
                editedSdkConfig.SanitizeBaseUrl();
                editedSdkConfig.Expand(generateServiceUrl);

                originalClientOAuthConfigs = AccelByteSettingsV2.SetOAuthByEnvironment(originalClientOAuthConfigs, editedClientOAuthConfig, (SettingsEnvironment) temporaryEnvironmentSetting);
                originalSdkConfigs = AccelByteSettingsV2.SetSDKConfigByEnvironment(originalSdkConfigs, editedSdkConfig, (SettingsEnvironment)temporaryEnvironmentSetting);
                originalAnalyticsOAuthConfigs = AccelByteSettingsV2.SetOAuthByEnvironment(originalAnalyticsOAuthConfigs, editedAnalyticsOAuthConfig, (SettingsEnvironment)temporaryEnvironmentSetting);

                AccelByteSettingsV2.SaveConfig(originalClientOAuthConfigs, AccelByteSettingsV2.OAuthFullPath(GetPlatformName(platformList, temporaryPlatformSetting)));
                AccelByteSettingsV2.SaveConfig(originalSdkConfigs, AccelByteSettingsV2.SDKConfigFullPath(false));
                AccelByteSettingsV2.SaveConfig(originalAnalyticsOAuthConfigs, AccelByteAnalyticsSettings.AnalyticsOAuthFullPath(GetPlatformName(platformList, temporaryPlatformSetting)));
            }

            EditorGUILayout.EndVertical();
#endif
        }

        private void CreateNumberInput(Action<double> setter, double defaultValue, string fieldLabel, bool required = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(fieldLabel);
            var newValue = EditorGUILayout.DoubleField(defaultValue);
            setter?.Invoke(newValue);

            string requiredText = "";
            if (required)
            {
                requiredText = "Required";
            }
            EditorGUILayout.LabelField(requiredText, requiredTextFieldGUIStyle);

            EditorGUILayout.EndHorizontal();
        }

        private void CreateNumberInput(Action<float> setter, float defaultValue, string fieldLabel, bool required = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(fieldLabel);
            var newValue = EditorGUILayout.FloatField(defaultValue);
            setter?.Invoke(newValue);

            string requiredText = "";
            if (required)
            {
                requiredText = "Required";
            }
            EditorGUILayout.LabelField(requiredText, requiredTextFieldGUIStyle);

            EditorGUILayout.EndHorizontal();
        }

        private void CreateTextInput(Action<string> setter, string defaultValue, string fieldLabel, bool required = false, bool @readonly = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(fieldLabel);

            if (!@readonly)
            {
                var newValue = EditorGUILayout.TextField(defaultValue);
                setter?.Invoke(newValue);

                string requiredText = "";
                if (required && string.IsNullOrEmpty(newValue))
                {
                    requiredText = "Required";
                }
                EditorGUILayout.LabelField(requiredText, requiredTextFieldGUIStyle);
            }
            else
            {
                EditorGUILayout.LabelField(defaultValue);
                EditorGUILayout.LabelField(string.Empty, requiredTextFieldGUIStyle);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateToggleInput(Action<bool> setter, bool defaultValue, string fieldLabel)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(fieldLabel);
            var newValue = EditorGUILayout.Toggle(defaultValue);
            setter?.Invoke(newValue);

            EditorGUILayout.LabelField("");
            EditorGUILayout.EndHorizontal();
        }

        private bool CompareOAuthConfig(OAuthConfig firstConfig, OAuthConfig secondConfig)
        {
            if (firstConfig == null || secondConfig == null)
            {
                return false;
            }
            return firstConfig.Compare(secondConfig);
        }

        private bool CompareConfig(Config firstConfig, Config secondConfig)
        {
            if (firstConfig == null || secondConfig == null)
            {
                return false;
            }
            return firstConfig.Compare(secondConfig);
        }

        private string GetPlatformName(string[] platformList, int index)
        {
            string targetPlatform = "";
            if (platformList[index] != "Default")
            {
                targetPlatform = platformList[index];
            }
            return targetPlatform;
        }
    }
}
