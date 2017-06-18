﻿#region License

// Copyright (c) 2014 The Sentry Team and individual contributors.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted
// provided that the following conditions are met:
// 
//     1. Redistributions of source code must retain the above copyright notice, this list of
//        conditions and the following disclaimer.
// 
//     2. Redistributions in binary form must reproduce the above copyright notice, this list of
//        conditions and the following disclaimer in the documentation and/or other materials
//        provided with the distribution.
// 
//     3. Neither the name of the Sentry nor the names of its contributors may be used to
//        endorse or promote products derived from this software without specific prior written
//        permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
// ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Collections.Generic;
#if net35
using System.Web;
#endif
namespace SharpRaven.Data
{
    /// <summary>
    /// Static utility class for converting an HTTP request body through implementations of
    /// the <see cref="IHttpRequestBodyConverter"/> interface.
    /// </summary>
    public static class HttpRequestBodyConverter
    {
        /// <summary>
        /// Converts the HTTP request body of the specified <paramref name="httpContext"/> to
        /// a structured type.
        /// </summary>
        /// <param name="httpContext">The HTTP context containing the request body to convert.</param>
        /// <returns>
        /// A structured type for the specified <paramref name="httpContext"/>'s request body
        /// or <c>null</c> if the <paramref name="httpContext"/> is null, or the somehow conversion fails.
        /// </returns>
        #if net35
        public static object Convert(HttpContext httpContext)
        #else
        public static object Convert(dynamic httpContext)
        #endif
        {
            var mediaTypes = new Dictionary<string, IHttpRequestBodyConverter>
            {
                { "FormMediaType", new FormHttpRequestBodyConverter() },
                { "MultiPartFormMediaType", new MultiPartFormHttpRequestBodyConverter() },
                { "JsonMediaType", new JsonHttpRequestBodyConverter() },
                { "DefaultMediaType", new DefaultHttpRequestBodyConverter() }
            };

            foreach (var item in mediaTypes)
            {
                var mediaType = item.Value;

                if (!mediaType.Matches(httpContext.Request.ContentType))
                    continue;

                object data;
                if (mediaType.TryConvert(httpContext, out data))
                    return data;
            }

            return null;
        }
    }
}