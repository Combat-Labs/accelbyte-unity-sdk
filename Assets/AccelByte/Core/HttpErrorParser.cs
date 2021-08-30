﻿// Copyright (c) 2021 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Net;
using AccelByte.Models;

namespace AccelByte.Core
{
    public static class HttpErrorParser
    {
        public static Result TryParse(this IHttpResponse response)
        {
            Error error = ParseError(response);

            if (error != null) return Result.CreateError(error);

            return Result.CreateOk();
        }

        public static Result<T> TryParseJson<T>(this IHttpResponse response)
        {
            Error error = ParseError(response);

            if (error != null) return Result<T>.CreateError(error);

            try
            {
                if (response.BodyBytes == null) return Result<T>.CreateOk(default);
                
                return Result<T>.CreateOk(response.BodyBytes.ToObject<T>());
            }
            catch (Exception e)
            {
                return Result<T>.CreateError(ErrorCode.ErrorFromException, e.Message);
            }
        }

        private static Error ParseError(IHttpResponse response)
        {
            if (response == null) return new Error(ErrorCode.NetworkError, "There is no response.");

            if (response.Code >= 200 && response.Code < 300) return null;

            if (response.Code < 400 || response.Code >= 600) return HttpErrorParser.ParseDefaultError(response);
            
            if (response.BodyBytes == null) return new Error((ErrorCode)response.Code);

            try
            {
                return HttpErrorParser.ParseServiceError(response);
            }
            catch (Exception)
            {
                return new Error((ErrorCode)response.Code);
            }
        }

        private static Error ParseServiceError(IHttpResponse response)
        {
            var error = response.BodyBytes.ToObject<ServiceError>();

            if (error.numericErrorCode != 0)
            {
                return new Error((ErrorCode)error.numericErrorCode, error.errorMessage, error.messageVariables);
            }

            if (error.errorCode != 0)
            {
                return new Error((ErrorCode)error.errorCode, error.errorMessage, error.messageVariables);
            }

            if (error.code != 0)
            {
                return new Error((ErrorCode)error.code, error.message);
            }

            if (error.error != null)
            {
                string message = error.error;

                if (error.error_description != null) message += ": " + error.error_description;

                return new Error((ErrorCode)response.Code, message);
            }

            return new Error((ErrorCode)response.Code);
        }

        private static Error ParseDefaultError(IHttpResponse response)
        {
            if (response.BodyBytes == null) return new Error((ErrorCode)response.Code);

            string body = System.Text.Encoding.UTF8.GetString(response.BodyBytes);

            return new Error((ErrorCode)response.Code, "Unknown error: " + body);
        }
    }
}