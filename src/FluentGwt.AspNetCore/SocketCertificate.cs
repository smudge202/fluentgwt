using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace FluentGwt;

internal static class SocketCertificate
{
	private const string ServerAuthentication = "1.3.6.1.5.5.7.3.1";

	public static X509Certificate2 Create(string name)
	{
		using var key = RSA.Create(2048);
		var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		var alternativeNames = new SubjectAlternativeNameBuilder();
		alternativeNames.AddIpAddress(IPAddress.Loopback);
		alternativeNames.AddDnsName("localhost");
		request.CertificateExtensions.Add(alternativeNames.Build());
		request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid(ServerAuthentication)], critical: false));
		using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
		// SslStream on Windows cannot serve a certificate whose key is ephemeral, which is what CreateSelfSigned
		// produces; a round trip through PKCS#12 gives it a key Schannel accepts, without ever touching disk.
		return X509CertificateLoader.LoadPkcs12(created.Export(X509ContentType.Pkcs12), null);
	}
}
