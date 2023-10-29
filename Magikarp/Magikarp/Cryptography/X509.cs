using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cryptography
{
    public class X509
    {
        // Create x509 certificate from ECParameters
        internal static Byte[] CreateX509Certificate(ECParameters ecp, String SubjectName)
        {
            using (ECDsa ecdsa = ECDsa.Create(ecp))
            {
                // Create certificate request
                CertificateRequest req = new CertificateRequest(
                    $"CN={SubjectName}",
                    ecdsa,
                    HashAlgorithmName.SHA256);

                // Set extensions for key usages
                req.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature |
                        X509KeyUsageFlags.KeyCertSign |
                        X509KeyUsageFlags.DataEncipherment,
                        critical: true));

                // Create self-signed certificate
                X509Certificate2 certificate = req.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddYears(3));

                // Export to PFX format
                return certificate.Export(X509ContentType.Pfx);
            }
        }
    }
}