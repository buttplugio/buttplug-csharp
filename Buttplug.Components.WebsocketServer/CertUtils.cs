using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace Buttplug.Components.WebsocketServer
{
    internal static class CertUtils
    {
        // Note: Much of this code comes from https://stackoverflow.com/a/22247129
        private static X509Certificate2 GenerateSelfSignedCertificate(string subject)
        {
            const int keyStrength = 2048;

            // Generating Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            var certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            certificateGenerator.SetSerialNumber(BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random));

            // Subject Public Key
            var keyPairGenerator = new RsaKeyPairGenerator();
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            keyPairGenerator.Init(keyGenerationParameters);
            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // Subject DN
            certificateGenerator.SetSubjectDN(new X509Name("CN=" + subject));

            // Subject Alternative Name
            var subjectAlternativeNames = new List<Asn1Encodable>()
            {
                new GeneralName(GeneralName.DnsName, Environment.MachineName),
                new GeneralName(GeneralName.DnsName, "localhost"),
                new GeneralName(GeneralName.IPAddress, "127.0.0.1"),
            };

            if (subject != "localhost" && subject != Environment.MachineName)
            {
                subjectAlternativeNames.Add(new GeneralName(GeneralName.DnsName, subject));
            }

            certificateGenerator.AddExtension(X509Extensions.SubjectAlternativeName.Id, false, new DerSequence(subjectAlternativeNames.ToArray()));

            // Issuer
            certificateGenerator.SetIssuerDN(new X509Name("CN=" + subject));
            certificateGenerator.AddExtension(X509Extensions.SubjectKeyIdentifier.Id, false, new SubjectKeyIdentifier(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(subjectKeyPair.Public)));

            // Valid For
            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(2);
            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            // Add basic constraint
            certificateGenerator.AddExtension(X509Extensions.BasicConstraints.Id, true, new BasicConstraints(false));

            certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage.Id, false, new ExtendedKeyUsage(KeyPurposeID.IdKPServerAuth));

            // Signature Algorithm
            const string signatureAlgorithm = "SHA256WithRSA";
            var signatureFactory = new Asn1SignatureFactory(signatureAlgorithm, subjectKeyPair.Private);

            // selfsign certificate
            var certificate = certificateGenerator.Generate(signatureFactory);

            // correcponding private key
            var info = PrivateKeyInfoFactory.CreatePrivateKeyInfo(subjectKeyPair.Private);

            // merge into X509Certificate2
            var x509 = new X509Certificate2(certificate.GetEncoded());
            var seq = (Asn1Sequence)Asn1Object.FromByteArray(info.ParsePrivateKey().GetDerEncoded());
            if (seq.Count != 9)
            {
                // throw new PemException("malformed sequence in RSA private key");
            }

            var rsa = RsaPrivateKeyStructure.GetInstance(seq);
            var rsaparams = new RsaPrivateCrtKeyParameters(
                rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent, rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2, rsa.Coefficient);
            try
            {
                x509.PrivateKey = ToDotNetKey(rsaparams); // x509.PrivateKey = DotNetUtilities.ToRSA(rsaparams);
            }
            catch (CryptographicException e)
            {
                throw new Exception($"Exception on cert generation!\nSubject {subject}\nHostname {Environment.MachineName}\nSequenceCount {seq.Count} (should be 9?)", e);
            }

            return x509;
        }

        private static AsymmetricAlgorithm ToDotNetKey(RsaPrivateCrtKeyParameters privateKey)
        {
            var cspParams = new CspParameters
            {
                KeyContainerName = Guid.NewGuid().ToString(),
                KeyNumber = (int)KeyNumber.Exchange,
                Flags = CspProviderFlags.UseMachineKeyStore,
            };
            var rsaProvider = new RSACryptoServiceProvider(cspParams);
            var parameters = new RSAParameters
            {
                Modulus = privateKey.Modulus.ToByteArrayUnsigned(),
                P = privateKey.P.ToByteArrayUnsigned(),
                Q = privateKey.Q.ToByteArrayUnsigned(),
                DP = privateKey.DP.ToByteArrayUnsigned(),
                DQ = privateKey.DQ.ToByteArrayUnsigned(),
                InverseQ = privateKey.QInv.ToByteArrayUnsigned(),
                D = privateKey.Exponent.ToByteArrayUnsigned(),
                Exponent = privateKey.PublicExponent.ToByteArrayUnsigned(),
            };
            rsaProvider.ImportParameters(parameters);
            return rsaProvider;
        }

        public static X509Certificate2 GetCert(string app, string hostname = "localhost")
        {
            var appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), app);
            var caPfx = Path.Combine(appPath, "ca.pfx");
            var certPfx = Path.Combine(appPath, "cert.pfx");

            // Patch release rework of cert handling: our websocket server doesn't acept a chain!
            if (File.Exists(caPfx))
            {
                File.Delete(caPfx);
                if (File.Exists(certPfx))
                {
                    File.Delete(certPfx);
                }
            }

            if (File.Exists(certPfx))
            {
                return new X509Certificate2(File.ReadAllBytes(certPfx), (string)null,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            }

            var clientCert = GenerateSelfSignedCertificate(hostname);
            var p12Cert = clientCert.Export(X509ContentType.Pfx);
            Directory.CreateDirectory(appPath);
            var w = File.OpenWrite(certPfx);
            w.Write(p12Cert, 0, p12Cert.Length);
            w.Close();

            return new X509Certificate2(File.ReadAllBytes(certPfx), (string)null,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }
    }
}
