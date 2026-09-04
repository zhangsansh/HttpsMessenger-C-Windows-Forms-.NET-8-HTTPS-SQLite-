using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace HttpsMessenger.Services;

/// <summary>
/// 自签名 HTTPS 证书的创建、加载与导出。
/// </summary>
public static class CertificateHelper
{
    public static string DefaultCertDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HttpsMessenger");

    public static string DefaultCertPath => Path.Combine(DefaultCertDirectory, "server.pfx");

    public const string DefaultPassword = "HttpsMessenger@2026";

    /// <summary>
    /// 加载已有证书；若不存在则自动创建自签名证书。
    /// </summary>
    public static X509Certificate2 LoadOrCreate(string? pfxPath = null, string? password = null)
    {
        pfxPath ??= DefaultCertPath;
        password ??= DefaultPassword;

        Directory.CreateDirectory(Path.GetDirectoryName(pfxPath)!);

        if (File.Exists(pfxPath))
        {
            return new X509Certificate2(
                pfxPath,
                password,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        var cert = CreateSelfSigned("CN=HttpsMessenger-Local");
        ExportPfx(cert, pfxPath, password);
        return cert;
    }

    /// <summary>
    /// 创建 RSA 自签名证书（用于本机 HTTPS 演示）。
    /// </summary>
    public static X509Certificate2 CreateSelfSigned(string subjectName, int yearsValid = 5)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            subjectName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: true));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                critical: false));

        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddDnsName("127.0.0.1");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
        sanBuilder.AddIpAddress(System.Net.IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = notBefore.AddYears(yearsValid);
        using var cert = request.CreateSelfSigned(notBefore, notAfter);

        // 导出再导入，确保带有私钥且可持久使用
        var pfxBytes = cert.Export(X509ContentType.Pfx, DefaultPassword);
        return new X509Certificate2(
            pfxBytes,
            DefaultPassword,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }

    public static void ExportPfx(X509Certificate2 certificate, string path, string password)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
    }

    public static void ExportCer(X509Certificate2 certificate, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Cert));
    }

    public static string Describe(X509Certificate2 certificate)
    {
        return $"主题: {certificate.Subject}\r\n" +
               $"指纹: {certificate.Thumbprint}\r\n" +
               $"有效期: {certificate.NotBefore:yyyy-MM-dd} ~ {certificate.NotAfter:yyyy-MM-dd}\r\n" +
               $"是否含私钥: {(certificate.HasPrivateKey ? "是" : "否")}";
    }
}
