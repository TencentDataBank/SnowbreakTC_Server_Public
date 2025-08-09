using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SnowbreakTC.Server.Sdk;

public static class Certs
{
    private static readonly ConcurrentDictionary<string, X509Certificate2> Cache = new();
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static X509Certificate2 GetOrCreate(string dir, string? sni)
    {
        Directory.CreateDirectory(dir);
        var host = string.IsNullOrWhiteSpace(sni) ? "snowbreaktc" : sni.ToLowerInvariant();
        var subject = $"CN={host}, OU={SnowbreakTC.Core.Cluster.CertOu}";
        return Cache.GetOrAdd(subject, _ => Build(dir, subject));
    }

    private static X509Certificate2 Build(string dir, string subject)
    {
        var file = Path.Combine(dir, $"{subject.Replace('=', '_').Replace('*', '_').Replace('/', '_').Replace(", ", "@")}.pfx");
        Gate.Wait();
        try
        {
            if (File.Exists(file))
            {
                var existing = new X509Certificate2(file);
                if (DateTime.Now < existing.NotAfter.AddDays(-1))
                    return existing;
            }
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            var san = new SubjectAlternativeNameBuilder();
            var host = subject[3..];
            san.AddDnsName(host);
            var dot = host.IndexOf('.');
            if (dot > 0)
                san.AddDnsName("*" + host[dot..]);
            request.CertificateExtensions.Add(san.Build());
            var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
            var pfx = cert.Export(X509ContentType.Pfx);
            File.WriteAllBytes(file, pfx);
            return new X509Certificate2(pfx);
        }
        finally
        {
            Gate.Release();
        }
    }
}
