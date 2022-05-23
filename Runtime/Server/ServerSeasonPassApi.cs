﻿// Copyright (c) 2021 - 2022 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine.Assertions;

namespace AccelByte.Server
{
    internal class ServerSeasonPassApi : ServerApiBase
    {
        /// <summary>
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config">baseUrl==BaseUrl</param> // TODO: Should this base BaseUrl?
        /// <param name="session"></param>
        internal ServerSeasonPassApi( IHttpClient httpClient
            , ServerConfig config
            , ISession session ) 
            : base( httpClient, config, config.BaseUrl, session )
        {
        }

        public IEnumerator GrantExpToUser( string userId
            , int exp
            , ResultCallback<UserSeasonInfoWithoutReward> callback )
        {
            Report.GetFunctionLog(GetType().Name);
            Assert.IsNotNull(Namespace_, "Can't Grant Exp! Namespace parameter is null!");
            Assert.IsNotNull(AuthToken, "Can't Grant Exp! AccessToken parameter is null!");
            Assert.IsNotNull(userId, "Can't Grant Exp! UserId parameter is null!");

            var request = HttpRequestBuilder
                .CreatePost(BaseUrl + "/seasonpass/admin/namespaces/{namespace}/users/{userId}/seasons/current/exp")
                .WithPathParam("namespace", Namespace_)
                .WithPathParam("userId", userId)
                .WithBearerAuth(AuthToken)
                .WithContentType(MediaType.ApplicationJson)
                .WithBody(string.Format("{{ \"exp\": {0} }}", exp))
                .Accepts(MediaType.ApplicationJson)
                .GetResult();

            IHttpResponse response = null;

            yield return HttpClient.SendRequest(request, 
                rsp => response = rsp);

            var result = response.TryParseJson<UserSeasonInfoWithoutReward>();
            callback.Try(result);
        }

        public IEnumerator GetCurrentUserSeasonProgression( string userId
            ,  ResultCallback<UserSeasonInfoWithoutReward> callback )
        {
            Report.GetFunctionLog(GetType().Name);
            Assert.IsNotNull(Namespace_, "Can't check user progression! Namespace parameter is null!");
            Assert.IsNotNull(AuthToken, "Can't check user progression! AccessToken parameter is null!");
            Assert.IsNotNull(userId, "Can't check user progression! UserId parameter is null!");

            var request = HttpRequestBuilder
                .CreateGet(BaseUrl + "/seasonpass/admin/namespaces/{namespace}/users/{userId}/seasons/current/progression")
                .WithPathParam("namespace", Namespace_)
                .WithPathParam("userId", userId)
                .WithBearerAuth(AuthToken)
                .WithContentType(MediaType.ApplicationJson)
                .Accepts(MediaType.ApplicationJson)
                .GetResult();

            IHttpResponse response = null;

            yield return HttpClient.SendRequest(request, 
                rsp => response = rsp);

            var result = response.TryParseJson<UserSeasonInfoWithoutReward>();
            callback.Try(result);
        }
    }
}
