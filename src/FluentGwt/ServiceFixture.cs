using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentGwt;

public abstract class ServiceFixture : IAsyncDisposable
{
	private readonly Lock _lock = new();
	private readonly ServiceCollection _services = [];
	private readonly List<Type> _resolved = [];
	private readonly List<Func<CancellationToken, ValueTask>> _teardown = [];
	private readonly CancellationTokenSource _cancellation = new();
	private readonly TestConfiguration _configuration;
	private ServiceProvider? _provider;
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

	protected ServiceFixture() =>
		_configuration = new(GetType().Assembly);

	public IConfiguration Configuration => _configuration.Root;

	public CancellationToken Cancellation => _cancellation.Token;

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
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (AggregateException)
		{
			return false;
		}
	}

	public void Configure(string key, string? value) => _configuration.Set(key, value);

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

	internal async ValueTask<IReadOnlyList<Exception>> TearDown()
	{
		await _cancellation.CancelAsync();
		List<Func<CancellationToken, ValueTask>> callbacks;
		lock (_lock)
		{
			callbacks = [.. _teardown];
			_teardown.Clear();
		}
		callbacks.Reverse();
		var failures = new List<Exception>();
		foreach (var callback in callbacks)
			await Collecting(failures, () => callback(CancellationToken.None));
		if (_provider is not null)
			await Collecting(failures, () => _provider.DisposeAsync());
		await Collecting(failures, DisposeFixture);
		_cancellation.Dispose();
		return failures;
	}

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

	private ServiceProvider Provider(Type resolving)
	{
		lock (_lock)
		{
			_resolved.Add(resolving);
			_services.TryAddSingleton(Configuration);
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
