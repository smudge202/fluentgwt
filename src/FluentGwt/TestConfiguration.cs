using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace FluentGwt;

internal sealed class TestConfiguration
{
	private static readonly ConcurrentDictionary<Assembly, IConfiguration> _Settings = new();

	private readonly MemoryConfigurationProvider _overlay;

	public TestConfiguration(Assembly testAssembly)
	{
		var root = new ConfigurationBuilder()
			.AddConfiguration(_Settings.GetOrAdd(testAssembly, Settings), shouldDisposeConfiguration: false)
			.AddInMemoryCollection()
			.Build();
		_overlay = root.Providers.OfType<MemoryConfigurationProvider>().Single();
		Root = root;
	}

	public IConfiguration Root { get; }

	public void Set(string key, string? value) => _overlay.Set(key, value);

	private static IConfiguration Settings(Assembly testAssembly)
	{
		var builder = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", optional: true);
		var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
			?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
		if (environment is not null)
			builder.AddJsonFile($"appsettings.{environment}.json", optional: true);
		return builder
			.AddUserSecrets(testAssembly, optional: true)
			.AddEnvironmentVariables()
			.Build();
	}
}
