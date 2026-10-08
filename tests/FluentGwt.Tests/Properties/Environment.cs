using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace FluentGwt.Tests.Properties;

internal static class Environment
{
	[ModuleInitializer]
	[SuppressMessage("Usage", "CA2255", Justification = "Configuration is built once per process, so the environment variable that proves precedence must exist before any test runs.")]
	public static void SetOverridingVariable() =>
		System.Environment.SetEnvironmentVariable("FluentGwtTests__Overridden", "environment");
}
