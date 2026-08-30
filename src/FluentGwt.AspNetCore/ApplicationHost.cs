using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentGwt;

public sealed partial class ApplicationHost : FixtureHost
{
	private static readonly ConditionalWeakTable<ServiceFixture, List<ApplicationHost>> _Hosts = new();

	private readonly Lock _lock = new();
	private readonly ServiceFixture _fixture;
	private readonly Func<ApplicationHost, Running> _start;
	private readonly List<Action<IServiceCollection>> _services = [];
	private readonly Dictionary<string, string?> _configuration = [];
	private readonly List<Action<IApplicationBuilder>> _pipeline = [];
	private string _environment = Environments.Development;
	private Running? _running;
	private TestAuthentication? _authentication;
	private TestAuthorisation? _authorisation;

	[SuppressMessage("Globalization", "CA1308", Justification = "Host names in a URI are lowercase; the result is only ever used as one.")]
	private ApplicationHost(ServiceFixture fixture, string name, Func<ApplicationHost, Running> start)
	{
		_fixture = fixture;
		_start = start;
		Name = name;
		Address = new($"http://{UnsafeInHostName().Replace(name.ToLowerInvariant(), "-")}/");
		fixture.Attach(this);
		var hosts = _Hosts.GetOrCreateValue(fixture);
		lock (hosts)
			hosts.Add(this);
	}

	public string Name { get; }

	public Uri Address { get; }

	public IServiceProvider Services => Ensure().Services;

	public TestAuthorisation Authorisation =>
		_authorisation ?? throw new InvalidOperationException($"The {Name} host records authorisation decisions only once Authentication() has been called while arranging it.");

	public static ApplicationHost For<EntryPoint>(ServiceFixture fixture) where EntryPoint : class
	{
		ArgumentNullException.ThrowIfNull(fixture);
		return new(fixture, typeof(EntryPoint).Name, host => host.StartEntryPoint<EntryPoint>());
	}

	public static ApplicationHost Composed(ServiceFixture fixture, string name, Action<WebApplicationBuilder> services, Action<WebApplication> pipeline)
	{
		ArgumentNullException.ThrowIfNull(fixture);
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(pipeline);
		return new(fixture, name, host => host.StartComposed(services, pipeline));
	}

	public TestAuthentication Authentication()
	{
		lock (_lock)
		{
			if (_authentication is not null)
				return _authentication;
		}
		var authentication = new TestAuthentication(_fixture);
		var authorisation = new TestAuthorisation();
		Arranging(() =>
		{
			_authentication = authentication;
			_authorisation = authorisation;
			_services.Add(services => StubAuthentication.Apply(services, authentication, authorisation));
		});
		return authentication;
	}

	public ApplicationHost ConfigureServices(Action<IServiceCollection> configure)
	{
		ArgumentNullException.ThrowIfNull(configure);
		return Arranging(() => _services.Add(configure));
	}

	public ApplicationHost Configure(string key, string? value)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		return Arranging(() => _configuration[key] = value);
	}

	public ApplicationHost Pipeline(Action<IApplicationBuilder> configure)
	{
		ArgumentNullException.ThrowIfNull(configure);
		return Arranging(() => _pipeline.Add(configure));
	}

	public ApplicationHost Environment(string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		return Arranging(() => _environment = name);
	}

	public Service Resolve<Service>() where Service : notnull => Services.GetRequiredService<Service>();

	public async Task Scope(Action<IServiceProvider> work)
	{
		ArgumentNullException.ThrowIfNull(work);
		await using var scope = Services.CreateAsyncScope();
		work(scope.ServiceProvider);
	}

	public async Task Scope(Func<IServiceProvider, CancellationToken, Task> work)
	{
		ArgumentNullException.ThrowIfNull(work);
		await using var scope = Services.CreateAsyncScope();
		await work(scope.ServiceProvider, _fixture.Cancellation);
	}

	public async Task<Result> Scope<Result>(Func<IServiceProvider, CancellationToken, Task<Result>> work)
	{
		ArgumentNullException.ThrowIfNull(work);
		await using var scope = Services.CreateAsyncScope();
		return await work(scope.ServiceProvider, _fixture.Cancellation);
	}

	public HttpClient CreateClient() => new(CreateHandler()) { BaseAddress = Address };

	internal HttpMessageHandler CreateHandler() => Ensure().Server.CreateHandler();

	public ValueTask Start(CancellationToken cancellationToken)
	{
		Ensure();
		return ValueTask.CompletedTask;
	}

	public async ValueTask DisposeAsync()
	{
		Running? running;
		lock (_lock)
			running = _running;
		if (running is not null)
			await running.Dispose();
	}

	internal static ApplicationHost Only(ServiceFixture fixture)
	{
		if (!_Hosts.TryGetValue(fixture, out var hosts))
			throw new InvalidOperationException($"{fixture.GetType().Name} has no application host to arrange.");
		lock (hosts)
			return hosts.Count == 1
				? hosts[0]
				: throw new InvalidOperationException(
					$"{fixture.GetType().Name} has {hosts.Count} application hosts ({string.Join(", ", hosts.Select(x => x.Name))}); arrange the one meant through it, as x.Host.Authentication().");
	}

	private Running Ensure()
	{
		lock (_lock)
			return _running ??= _start(this);
	}

	private ApplicationHost Arranging(Action change)
	{
		lock (_lock)
		{
			if (_running is not null)
				throw new InvalidOperationException(
					$"The {Name} host has started, so it can no longer be arranged. Arrange it in an immediate given; hosts start after those.");
			change();
		}
		return this;
	}

	private void Overrides(IServiceCollection services)
	{
		services.Override<TimeProvider>(_fixture.Time);
		foreach (var configure in _services)
			configure(services);
	}

	[SuppressMessage("Reliability", "CA2000", Justification = "Both factories are disposed by the Running they are handed to, or in the catch when start fails.")]
	private Running StartEntryPoint<EntryPoint>() where EntryPoint : class
	{
		var factory = new WebApplicationFactory<EntryPoint>();
		var configured = factory.WithWebHostBuilder(builder =>
		{
			builder.UseEnvironment(_environment);
			foreach (var (key, value) in _configuration)
				builder.UseSetting(key, value);
			builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new PipelineFilter(_pipeline)));
			builder.ConfigureTestServices(Overrides);
		});
		try
		{
			var server = configured.Server;
			server.BaseAddress = Address;
			return new(server, configured.Services, () => factory.DisposeAsync());
		}
		catch
		{
			configured.Dispose();
			factory.Dispose();
			throw;
		}
	}

	private Running StartComposed(Action<WebApplicationBuilder> services, Action<WebApplication> pipeline)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = _environment });
		builder.WebHost.UseTestServer();
		builder.Configuration.AddInMemoryCollection(_configuration);
		services(builder);
		Overrides(builder.Services);
		var app = builder.Build();
		try
		{
			foreach (var configure in _pipeline)
				configure(app);
			pipeline(app);
			app.Start();
			var server = app.GetTestServer();
			server.BaseAddress = Address;
			return new(server, app.Services, async () =>
			{
				await app.StopAsync();
				await app.DisposeAsync();
			});
		}
		catch
		{
			((IDisposable)app).Dispose();
			throw;
		}
	}

	[GeneratedRegex("[^a-z0-9-]")]
	private static partial Regex UnsafeInHostName();

	private sealed class Running(TestServer server, IServiceProvider services, Func<ValueTask> dispose)
	{
		public TestServer Server => server;

		public IServiceProvider Services => services;

		public ValueTask Dispose() => dispose();
	}

	private sealed class PipelineFilter(IReadOnlyList<Action<IApplicationBuilder>> pipeline) : IStartupFilter
	{
		public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
			app =>
			{
				foreach (var configure in pipeline)
					configure(app);
				next(app);
			};
	}
}
