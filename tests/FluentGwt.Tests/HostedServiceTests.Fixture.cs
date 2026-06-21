using FluentGwt.Tests.Integrated;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentGwt.Tests;

public sealed partial class HostedServiceTests
{
	private static Type BackgroundType { get; } = typeof(Background);

	private Fixture Context { get; } = new();

	internal sealed class Fixture
	{
		public IntegratedFixture Integrated { get; } = new();
		public PlainFixture Plain { get; } = new();
		public ServiceCollection Collection { get; } = [];
		public Exception StartFailure { get; } = new InvalidOperationException("Start failure");
		public Func<Task> Chain { get; set; } = () => Task.CompletedTask;
		public Exception? Failure { get; set; }
		public List<CancellationToken> Tokens { get; } = [];
	}

	internal sealed class PlainFixture : ServiceFixture
	{
		public List<string> Log { get; } = [];
	}

	internal sealed class Recording(string name, ICollection<string> log) : IHostedService
	{
		public Task StartAsync(CancellationToken cancellationToken)
		{
			log.Add($"start {name}");
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			log.Add($"stop {name}");
			return Task.CompletedTask;
		}
	}

	internal sealed class FailingToStart(Exception failure) : IHostedService
	{
		public Task StartAsync(CancellationToken cancellationToken) => Task.FromException(failure);

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}

	internal sealed class Lifecycle(ICollection<string> log) : IHostedLifecycleService
	{
		public Task StartingAsync(CancellationToken cancellationToken) => Record("starting");

		public Task StartAsync(CancellationToken cancellationToken) => Record("start");

		public Task StartedAsync(CancellationToken cancellationToken) => Record("started");

		public Task StoppingAsync(CancellationToken cancellationToken) => Record("stopping");

		public Task StopAsync(CancellationToken cancellationToken) => Record("stop");

		public Task StoppedAsync(CancellationToken cancellationToken) => Record("stopped");

		private Task Record(string step)
		{
			log.Add(step);
			return Task.CompletedTask;
		}
	}

	internal sealed class TokenRecorder(List<CancellationToken> tokens) : IHostedService
	{
		public Task StartAsync(CancellationToken cancellationToken)
		{
			tokens.Add(cancellationToken);
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			tokens.Add(cancellationToken);
			return Task.CompletedTask;
		}
	}

	internal sealed class Background : IHostedService
	{
		public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}

	internal sealed class OtherBackground : IHostedService
	{
		public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
