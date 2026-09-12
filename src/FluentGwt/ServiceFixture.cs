using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace FluentGwt;

public abstract class ServiceFixture : IAsyncDisposable
{
	private readonly Lock _lock = new();
	private readonly ServiceCollection _services = [];
	private readonly List<Type> _resolved = [];
	private readonly List<Func<CancellationToken, ValueTask>> _teardown = [];
	private readonly CancellationTokenSource _cancellation = CancellationTokenSource.CreateLinkedTokenSource(Runner.Token);
	private readonly TestConfiguration _configuration;
	private readonly List<IHostedService> _started = [];
	private readonly List<FixtureHost> _hosts = [];
	private readonly Lazy<int> _seed;
	private readonly Lazy<FakeTimeProvider> _time;
	private readonly Action<string>? _output = Runner.Output;
	private readonly string? _identity = Runner.TestIdentity;
	private ServiceProvider? _provider;
	private string _seedOrigin = "fresh";
	private bool _composed;
	private bool _validateOnBuild = true;
	private bool _validateScopes = true;

	public IServiceCollection Services
	{
		get
		{
			lock (_lock)
			{
				ThrowIfResolved();
				return _services;
			}
		}
	}

	protected ServiceFixture()
	{
		_configuration = new(GetType().Assembly);
		_seed = new(ChooseSeed);
		_time = new(() => new FakeTimeProvider(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(Seed)));
	}

	public int Seed => _seed.Value;

	public FakeTimeProvider Time => _time.Value;

	public string TestId => TestIdentity.Derive(Seed, _identity ?? GetType().FullName ?? GetType().Name);

	public IConfiguration Configuration => _configuration.Root;

	public CancellationToken Cancellation => _cancellation.Token;

	public FakeLogCollector Logs { get; } = new();

	public bool IntegrationEnabled => GetType().Assembly.IsDefined(typeof(IntegrationEnabledAttribute), inherit: false);

	public bool ValidateOnBuild
	{
		get => _validateOnBuild;
		set
		{
			lock (_lock)
			{
				ThrowIfResolved();
				_validateOnBuild = value;
			}
		}
	}

	public bool ValidateScopes
	{
		get => _validateScopes;
		set
		{
			lock (_lock)
			{
				ThrowIfResolved();
				_validateScopes = value;
			}
		}
	}

	public Service Resolve<Service>() where Service : notnull =>
		Provider(typeof(Service)).GetRequiredService<Service>();

	public Service Resolve<Service>(object key) where Service : notnull =>
		Provider(typeof(Service)).GetRequiredKeyedService<Service>(key);

	public bool IsResolvable<Service>() where Service : notnull
	{
		// The container has no non-throwing way to ask whether it can construct a service or pass
		// validation; both are reported only by exception, so they are caught here and nowhere else.
		try
		{
			return Provider(typeof(Service)).GetService<Service>() is not null;
		}
		catch (InvalidOperationException failure)
		{
			return NotResolvable<Service>(failure);
		}
		catch (AggregateException failure)
		{
			return NotResolvable<Service>(failure);
		}
	}

	public void Configure(string key, string? value) => _configuration.Set(key, value);

	[SuppressMessage("Reliability", "CA2000", Justification = "Logging owns the providers it is given and disposes them with itself.")]
	public void CaptureLogs(ILoggingBuilder logging, string source)
	{
		ArgumentNullException.ThrowIfNull(logging);
		ArgumentException.ThrowIfNullOrWhiteSpace(source);
		logging
			.AddProvider(new FakeLoggerProvider(Logs))
			.AddProvider(new TestOutputLoggerProvider(Write, OutputLevel(), source));
	}

	public void Attach(FixtureHost host)
	{
		ArgumentNullException.ThrowIfNull(host);
		lock (_lock)
			_hosts.Add(host);
	}

	public void OnTeardown(Func<CancellationToken, ValueTask> callback)
	{
		ArgumentNullException.ThrowIfNull(callback);
		lock (_lock)
			_teardown.Add(callback);
	}

	public async ValueTask DisposeAsync()
	{
		var failures = await TearDown();
		GC.SuppressFinalize(this);
		Teardown.Rethrow(failures);
	}

	internal void ChooseSeedNow() => _ = Seed;

	internal void ReportSeed()
	{
		if (_seed.IsValueCreated)
			Write($"FluentGwt seed: {Seed} ({_seedOrigin}; replay with FluentGwtSeed={Seed})");
	}

	internal void Write(string line) => Runner.Write(_output, line);

	internal async Task StartHosts()
	{
		List<FixtureHost> hosts;
		lock (_lock)
			hosts = [.. _hosts];
		foreach (var host in hosts)
			await host.Start(Runner.Token);
	}

	internal async Task StartHostedServices()
	{
		if (!IntegrationEnabled)
			return;
		var services = Resolve<IEnumerable<IHostedService>>().ToList();
		foreach (var service in services.OfType<IHostedLifecycleService>())
			await service.StartingAsync(Runner.Token);
		foreach (var service in services)
		{
			await service.StartAsync(Runner.Token);
			_started.Add(service);
		}
		foreach (var service in services.OfType<IHostedLifecycleService>())
			await service.StartedAsync(Runner.Token);
	}

	internal async ValueTask<IReadOnlyList<Exception>> TearDown()
	{
		await _cancellation.CancelAsync();
		var failures = new List<Exception>();
		var started = Enumerable.Reverse(_started).ToList();
		_started.Clear();
		foreach (var service in started.OfType<IHostedLifecycleService>())
			await Collecting(failures, () => new ValueTask(service.StoppingAsync(Runner.Token)));
		foreach (var service in started)
			await Collecting(failures, () => new ValueTask(service.StopAsync(Runner.Token)));
		foreach (var service in started.OfType<IHostedLifecycleService>())
			await Collecting(failures, () => new ValueTask(service.StoppedAsync(Runner.Token)));
		List<Func<CancellationToken, ValueTask>> callbacks;
		lock (_lock)
		{
			callbacks = [.. _teardown];
			_teardown.Clear();
		}
		callbacks.Reverse();
		foreach (var callback in callbacks)
			await Collecting(failures, () => callback(Runner.Token));
		List<FixtureHost> hosts;
		lock (_lock)
		{
			hosts = [.. _hosts];
			_hosts.Clear();
		}
		hosts.Reverse();
		foreach (var host in hosts)
			await Collecting(failures, host.DisposeAsync);
		if (_provider is not null)
			await Collecting(failures, () => _provider.DisposeAsync());
		await Collecting(failures, DisposeFixture);
		_cancellation.Dispose();
		foreach (var failure in failures)
			Write($"FluentGwt teardown failure: {failure}");
		return failures;
	}

	protected virtual int? FixedSeed => null;

	protected virtual ValueTask DisposeFixture() => ValueTask.CompletedTask;

	[SuppressMessage("Design", "CA1031", Justification = "Every teardown step runs whatever an earlier one threw; the failures are collected and rethrown together.")]
	private static async ValueTask Collecting(List<Exception> failures, Func<ValueTask> step)
	{
		try
		{
			await step();
		}
		catch (Exception failure)
		{
			failures.Add(failure);
		}
	}

	private int ChooseSeed()
	{
		if (ForcedSeed.From(Configuration) is { } forced)
		{
			_seedOrigin = "forced";
			return forced;
		}
		if (FixedSeed is { } declared)
		{
			_seedOrigin = "declared";
			return declared;
		}
		return RandomNumberGenerator.GetInt32(int.MaxValue);
	}

	private bool NotResolvable<Service>(Exception failure)
	{
		Write($"FluentGwt: {typeof(Service).Name} is not resolvable. {failure.Message}");
		return false;
	}

	private LogLevel OutputLevel() =>
		Enum.TryParse<LogLevel>(Configuration["FluentGwt:LogLevel"], ignoreCase: true, out var configured)
			? configured
			: Debugger.IsAttached ? LogLevel.Debug : LogLevel.Warning;

	private ServiceProvider Provider(Type resolving)
	{
		lock (_lock)
		{
			_resolved.Add(resolving);
			if (!_composed)
			{
				_services.TryAddSingleton(Configuration);
				_services.TryAddSingleton<TimeProvider>(Time);
				_services.AddLogging(logging => logging
					.SetMinimumLevel(LogLevel.Trace)
					.AddProvider(new FakeLoggerProvider(Logs))
					.AddProvider(new TestOutputLoggerProvider(Write, OutputLevel())));
				_composed = true;
			}
			return _provider ??= _services.BuildServiceProvider(new ServiceProviderOptions
			{
				ValidateOnBuild = _validateOnBuild,
				ValidateScopes = _validateScopes,
			});
		}
	}

	private void ThrowIfResolved()
	{
		if (_provider is not null)
			throw new InvalidOperationException(
				$"Services cannot change once resolution has begun. Resolved so far: {string.Join(", ", _resolved.Distinct().Select(x => x.Name))}");
	}
}
