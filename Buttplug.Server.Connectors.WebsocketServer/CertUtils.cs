// <copyright file="CertUtils.cs" company="Nonpolynomial Labs LLC">
//     Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
//     Copyright (c) Nonpolynomial Labs LLC. All rights reserved. Licensed under the BSD 3-Clause
//     license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using IoTSharp.X509Extensions;

namespace Buttplug.Server.Connectors.WebsocketServer
{
    public static class CertUtils
    {
        public static X509Certificate2 LoadPEMCert(string aPEMCertPath, string aPEMPrivKeyPath)
        {
            if (!File.Exists(aPEMCertPath) || !File.Exists(aPEMPrivKeyPath))
            {
                throw new ArgumentException();
            }
            // Converting PEMs is an extension method that requires an instance but doesn't update itself. Oi.
            var cert = new X509Certificate2();
            return cert.LoadPem(aPEMCertPath, aPEMPrivKeyPath);
        }

        /// <exception cref="CryptographicException">
        /// Sometimes thrown due to issues generating keys.
        /// </exception>
        public static X509Certificate2 GetCert(string app)
        {
            var appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), app);
            var certPfx = Path.Combine(appPath, "cert.pfx");

            if (!File.Exists(certPfx))
            {
                throw new FileNotFoundException("cert.pfx file not found. You will need to generate a cert somehow.");
            }
            return new X509Certificate2(
                File.ReadAllBytes(certPfx),
                (string)null,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        // Self-signed cert generation is removed. Too hard to maintain. If you
        // need a cert, use openssl or Intiface.
    }
}
