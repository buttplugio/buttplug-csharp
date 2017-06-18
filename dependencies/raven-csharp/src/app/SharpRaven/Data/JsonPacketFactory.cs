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

using System;
using System.Collections.Generic;

namespace SharpRaven.Data
{
    /// <summary>
    /// A default implementation of <see cref="IJsonPacketFactory"/>. Override the <see cref="OnCreate"/>
    /// method to adjust the values of the <see cref="JsonPacket"/> before it is sent to Sentry.
    /// </summary>
    public class JsonPacketFactory : IJsonPacketFactory
    {
        /// <summary>
        /// Creates a new instance of
        /// <see cref="JsonPacket" /> for the specified
        /// <paramref name="project" />.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="message">The message to capture.</param>
        /// <param name="level">The <see cref="ErrorLevel" /> of the captured <paramref name="message" />. Default <see cref="ErrorLevel.Info" />.</param>
        /// <param name="tags">The tags to annotate the captured <paramref name="message" /> with.</param>
        /// <param name="fingerprint">The custom fingerprint to annotate the captured <paramref name="message" /> with.</param>
        /// <param name="extra">The extra metadata to send with the captured <paramref name="message" />.</param>
        /// <returns>
        /// A new instance of <see cref="JsonPacket" /> for the specified <paramref name="project" />.
        /// </returns>
        [Obsolete("Use Create(string, SentryEvent) instead.")]
        public JsonPacket Create(string project,
                                 SentryMessage message,
                                 ErrorLevel level = ErrorLevel.Info,
                                 IDictionary<string, string> tags = null,
                                 string[] fingerprint = null,
                                 object extra = null)
        {
            var @event = new SentryEvent(message)
            {
                Level = level,
                Extra = extra,
                Tags = tags,
                Fingerprint = fingerprint
            };

            return Create(project, @event);
        }


        /// <summary>
        /// Creates a new instance of
        /// <see cref="JsonPacket" /> for the specified
        /// <paramref name="project" />, with the
        /// given
        /// <paramref name="exception" />.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="exception">The <see cref="Exception" /> to capture.</param>
        /// <param name="message">The optional message to capture. Default: <see cref="Exception.Message" />.</param>
        /// <param name="level">The <see cref="ErrorLevel" /> of the captured <paramref name="exception" />. Default: <see cref="ErrorLevel.Error" />.</param>
        /// <param name="tags">The tags to annotate the captured <paramref name="exception" /> with.</param>
        /// <param name="fingerprint">The custom fingerprint to annotate the captured <paramref name="message" /> with.</param>
        /// <param name="extra">The extra metadata to send with the captured <paramref name="exception" />.</param>
        /// <returns>
        /// A new instance of
        /// <see cref="JsonPacket" /> for the specified
        /// <paramref name="project" />, with the
        /// given
        /// <paramref name="exception" />.
        /// </returns>
        [Obsolete("Use Create(string, SentryEvent) instead.")]
        public JsonPacket Create(string project,
                                 Exception exception,
                                 SentryMessage message = null,
                                 ErrorLevel level = ErrorLevel.Error,
                                 IDictionary<string, string> tags = null,
                                 string[] fingerprint = null,
                                 object extra = null)
        {
            var @event = new SentryEvent(exception)
            {
                Message = message,
                Level = level,
                Extra = extra,
                Tags = tags,
                Fingerprint = fingerprint,
            };

            return Create(project, @event);
        }


        /// <summary>
        /// Creates a new instance of  <see cref="JsonPacket" /> for the specified
        /// <paramref name="project" />, with the  given <paramref name="event" />.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="event">The event to capture.</param>
        /// <returns>
        /// A new instance of <see cref="JsonPacket" /> for the specified
        /// <paramref name="project" />, with the given <paramref name="event" />.
        /// </returns>
        public JsonPacket Create(string project, SentryEvent @event)
        {
            var json = new JsonPacket(project, @event)
            {
                Breadcrumbs = @event.Breadcrumbs
            };

            return OnCreate(json);
        }


        /// <summary>
        /// Called when the <see cref="JsonPacket"/> has been created. Can be overridden to
        /// adjust the values of the <paramref name="jsonPacket"/> before it is sent to Sentry.
        /// </summary>
        /// <param name="jsonPacket">The json packet.</param>
        /// <returns>
        /// The <see cref="JsonPacket"/>.
        /// </returns>
        protected virtual JsonPacket OnCreate(JsonPacket jsonPacket)
        {
            return jsonPacket;
        }
    }
}