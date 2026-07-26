using System.Collections.Concurrent;

namespace FluentGwt;

public sealed class HttpTraffic
{
	private readonly ConcurrentQueue<HttpRequestMessage> _requests = new();

	public IReadOnlyList<HttpRequestMessage> Requests => [.. _requests];

	internal void Record(HttpRequestMessage request) => _requests.Enqueue(request);
}
