namespace FluentGwt;

public sealed class HttpRedirection
{
	private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage?>>? _respond;

	public HttpRedirection RespondingWith(Func<HttpRequestMessage, HttpResponseMessage?> respond)
	{
		ArgumentNullException.ThrowIfNull(respond);
		_respond = (request, _) => Task.FromResult(respond(request));
		return this;
	}

	public HttpRedirection RespondingWith(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage?>> respond)
	{
		ArgumentNullException.ThrowIfNull(respond);
		_respond = respond;
		return this;
	}

	public HttpRedirection Via(HttpMessageHandler handler)
	{
		ArgumentNullException.ThrowIfNull(handler);
		_respond = async (request, cancellationToken) =>
		{
			using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
			return await invoker.SendAsync(request, cancellationToken);
		};
		return this;
	}

	internal HttpMessageHandler Handler(HttpTraffic traffic) => new RedirectingHandler(traffic, Respond);

	private Task<HttpResponseMessage?> Respond(HttpRequestMessage request, CancellationToken cancellationToken) =>
		_respond?.Invoke(request, cancellationToken) ?? Task.FromResult<HttpResponseMessage?>(null);

	private sealed class RedirectingHandler(HttpTraffic traffic, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage?>> respond)
		: HttpMessageHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			traffic.Record(request);
			return await respond(request, cancellationToken)
				?? throw new InvalidOperationException($"No redirected response matched {request.Method} {request.RequestUri}.");
		}
	}
}
