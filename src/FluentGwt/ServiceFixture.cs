using Microsoft.Extensions.DependencyInjection;

namespace FluentGwt;

public abstract class ServiceFixture : IAsyncDisposable
{
	private readonly Lock _lock = new();
	private readonly ServiceCollection _services = [];
	private readonly List<Type> _resolved = [];
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

	public async ValueTask DisposeAsync()
	{
		if (_provider is not null)
			await _provider.DisposeAsync();
		await DisposeFixture();
		GC.SuppressFinalize(this);
	}

	protected virtual ValueTask DisposeFixture() => ValueTask.CompletedTask;

	private ServiceProvider Provider(Type resolving)
	{
		lock (_lock)
		{
			_resolved.Add(resolving);
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
