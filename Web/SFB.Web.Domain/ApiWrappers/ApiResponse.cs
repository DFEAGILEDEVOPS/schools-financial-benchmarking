﻿using System.Net;

namespace SFB.Web.Domain
{
    public class ApiResponse
    {
        public readonly HttpStatusCode statusCode;
        public readonly dynamic responseObject;

        public ApiResponse(HttpStatusCode statusCode, dynamic responseObject)
        {
            this.statusCode = statusCode;
            this.responseObject = responseObject;
        }
    }
}
