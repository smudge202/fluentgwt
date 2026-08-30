using System.Diagnostics.CodeAnalysis;

namespace FluentGwt;

public static class HostRedirectionExtensions
{
	[SuppressMessage("Reliability", "CA2000", Justification = "The forwarding handler owns nothing; each request's handler is created and disposed within that request.")]
	public static HttpRedirection To(this HttpRedirection redirection, ApplicationHost host)
	{
		ArgumentNullException.ThrowIfNull(redirection);
		ArgumentNullException.ThrowIfNull(host);
		return redirection.Via(new HostForwardingHandler(host));
	}

	private sealed class HostForwardingHandler(ApplicationHost host) : HttpMessageHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (request.RequestUri is { } original)
				request.RequestUri = new Uri(host.Address, original.PathAndQuery);
			using var invoker = new HttpMessageInvoker(host.CreateHandler(), disposeHandler: true);
			return await invoker.SendAsync(request, cancellationToken);
		}
	}
}
