using System;
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
        private static X509Certificate2 GenerateSelfSignedCertificate(string subjectName, string issuerName, AsymmetricKeyParameter issuerPrivKey)
        {
            const int keyStrength = 2048;

            // Generating Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            var certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Signature Algorithm
            const string signatureAlgorithm = "SHA256WithRSA";
            var signatureFactory = new Asn1SignatureFactory(signatureAlgorithm, issuerPrivKey);

            // Issuer and Subject Name
            var subjectDN = new X509Name(subjectName);
            var issuerDN = new X509Name(issuerName);
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            // Valid For
            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(2);
            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            // Subject Public Key
            var keyPairGenerator = new RsaKeyPairGenerator();
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            keyPairGenerator.Init(keyGenerationParameters);
            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

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
            x509.PrivateKey = ToDotNetKey(rsaparams); // x509.PrivateKey = DotNetUtilities.ToRSA(rsaparams);
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

        // ReSharper disable once RedundantAssignment
        private static X509Certificate2 GenerateCACertificate(string subjectName, ref AsymmetricKeyParameter caPrivateKey)
        {
            const int keyStrength = 2048;

            // Generating Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            var certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Signature Algorithm
            const string signatureAlgorithm = "SHA256WithRSA";

            // Issuer and Subject Name
            var subjectDN = new X509Name(subjectName);
            var issuerDN = subjectDN;
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            // Valid For
            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(2);
            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            // Subject Public Key
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // Generating the Certificate
            var issuerKeyPair = subjectKeyPair;

            // selfsign certificate
            var signatureFactory = new Asn1SignatureFactory(signatureAlgorithm, issuerKeyPair.Private);
            var certificate = certificateGenerator.Generate(signatureFactory);
            var x509 = new X509Certificate2(certificate.GetEncoded());
            caPrivateKey = issuerKeyPair.Private;
            return x509;

            // return issuerKeyPair.Private;
        }

        public static X509Certificate2 GetCert(string app)
        {
            var appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), app);
            var caPfx = Path.Combine(appPath, "ca.pfx");
            var certPfx = Path.Combine(appPath, "cert.pfx");
            if (!File.Exists(caPfx) || !File.Exists(certPfx))
            {
                AsymmetricKeyParameter caPrivateKey = null;
                var caCert = GenerateCACertificate("CN=" + app + "CA", ref caPrivateKey);
                var clientCert = GenerateSelfSignedCertificate("CN=127.0.0.1", "CN=" + app + "CA", caPrivateKey);
                var p12ca = new X509Certificate2(caCert.Export(X509ContentType.Pfx), (string)null).Export(X509ContentType.Pfx);
                var p12cert = clientCert.Export(X509ContentType.Pfx);
                Directory.CreateDirectory(appPath);
                var w = File.OpenWrite(caPfx);
                w.Write(p12ca, 0, p12ca.Length);
                w.Close();
                w = File.OpenWrite(certPfx);
                w.Write(p12cert, 0, p12cert.Length);
                w.Close();
            }

            return new X509Certificate2(File.ReadAllBytes(certPfx), (string)null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }
    }
}
