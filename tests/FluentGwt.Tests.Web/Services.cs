using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace FluentGwt.Tests.Web;

public interface Greeter
{
	string Greet();
}

public sealed class FriendlyGreeter : Greeter
{
	public string Greet() => "Hello";
}

public sealed class RequestCounter
{
	private int _count;

	public int Next() => Interlocked.Increment(ref _count);
}

public sealed class Journal
{
	public ConcurrentQueue<string> Entries { get; } = new();
}

public sealed class StartupCheck(IConfiguration configuration, Journal journal) : IHostedService
{
	private int _stopped;

	private string Name => configuration["Web:Name"] ?? "web";

	public Task StartAsync(CancellationToken cancellationToken)
	{
		if (string.Equals(configuration["Web:FailOnStart"], "true", StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException($"{Name} was configured to fail on start.");
		journal.Entries.Enqueue($"start {Name}");
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		if (Interlocked.Exchange(ref _stopped, 1) == 0)
			journal.Entries.Enqueue($"stop {Name}");
		return Task.CompletedTask;
	}
}

public sealed class StampingResultHandler : IAuthorizationMiddlewareResultHandler
{
	private readonly AuthorizationMiddlewareResultHandler _default = new();

	public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
	{
		ArgumentNullException.ThrowIfNull(context);
		context.Response.Headers["X-Result-Handler"] = "application";
		return _default.HandleAsync(next, context, policy, authorizeResult);
	}
}
