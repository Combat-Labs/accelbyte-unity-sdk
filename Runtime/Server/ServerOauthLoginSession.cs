﻿// Copyright (c) 2020 - 2022 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;

namespace AccelByte.Server
{
    public class ServerOauthLoginSession : ISession
    {

        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string baseUrl;
        private readonly IHttpClient httpClient;

        internal ServerOauthLoginSession( string inBaseUrl
            , string inClientId
            , string inClientSecret
            , IHttpClient inHttpClient
            , CoroutineRunner inCoroutineRunner)
        {
            Assert.IsNotNull(inBaseUrl, $"Creating {GetType().Name} failed. Parameter inBaseUrl is null");
            Assert.IsNotNull(inClientId, "Creating " + GetType().Name + " failed. inClientId parameter is null!");
            Assert.IsNotNull(inClientSecret, "Creating " + GetType().Name + " failed. inClientSecret parameter is null!");
            Assert.IsNotNull(inHttpClient, "Creating " + GetType().Name + " failed. Parameter inHttpClient is null");
            Assert.IsNotNull(inCoroutineRunner, "Creating " + GetType().Name + " failed. Parameter inCoroutineRunner is null" );

            baseUrl = inBaseUrl;
            clientId = inClientId;
            clientSecret = inClientSecret;
            httpClient = inHttpClient;
            coroutineRunner = inCoroutineRunner;
        }

        public override string AuthorizationToken 
        { 
            get { return tokenData != null ? tokenData.access_token : null; }
            set { tokenData.access_token = value; }
        }

        public IEnumerator LoginWithClientCredentials( ResultCallback callback )
        {
            Result<TokenData> getClientTokenResult = null;

            yield return GetClientToken(r => getClientTokenResult = r);

            if (!getClientTokenResult.IsError)
            {
                SetSession(getClientTokenResult.Value);
                if (maintainAccessTokenCoroutine == null)
                {
                    maintainAccessTokenCoroutine = coroutineRunner.Run(MaintainToken());
                }

                callback.TryOk();
            }
            else
            {
                callback.TryError(getClientTokenResult.Error);
            }
        }

        private IEnumerator GetClientToken( ResultCallback<TokenData> callback )
        {
            IHttpRequest request = HttpRequestBuilder.CreatePost(baseUrl + "/v3/oauth/token")
                .WithBasicAuth(clientId, clientSecret)
                .WithContentType(MediaType.ApplicationForm)
                .Accepts(MediaType.ApplicationJson)
                .WithFormParam("grant_type", "client_credentials")
                .GetResult();

            IHttpResponse response = null;

            yield return httpClient.SendRequest(request, 
                rsp => response = rsp);

            Result<TokenData> result = response.TryParseJson<TokenData>();
            callback.Try(result);
        }

        public IEnumerator Logout( ResultCallback callback )
        {
            var request = HttpRequestBuilder.CreatePost(baseUrl + "/v3/oauth/revoke/token")
                .WithBearerAuth(AuthorizationToken)
                .WithContentType(MediaType.ApplicationForm)
                .Accepts(MediaType.ApplicationJson)
                .WithFormParam("token", AuthorizationToken)
                .GetResult();

            IHttpResponse response = null;

            yield return httpClient.SendRequest(request, 
                rsp => response = rsp);

            tokenData = null;
            var result = response.TryParse();
            coroutineRunner.Stop(maintainAccessTokenCoroutine);
            maintainAccessTokenCoroutine = null;
            callback.Try(result);
        }

        public override IEnumerator RefreshSessionApiCall(ResultCallback<TokenData, OAuthError> callback)
        {
            yield return GetClientToken(result=>
            {
                if (result.IsError || result.Value == null)
                {
                    var error = new OAuthError();
                    error.error = result.Error.Code.ToString();
                    error.error_description = result.Error.Message?.ToString();
                    callback.TryError(error);
                }
                else
                {
                    SetSession(result.Value);
                    callback.TryOk(result.Value);
                }
            });
        }

        public override void SetSession(TokenData loginResponse)
        {
            HttpRequestBuilder.SetNamespace(loginResponse.Namespace);
            tokenData = loginResponse;
            return;
        }
    }
}
