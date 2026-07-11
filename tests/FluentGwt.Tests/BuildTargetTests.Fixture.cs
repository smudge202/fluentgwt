using System.Diagnostics;

namespace FluentGwt.Tests;

public sealed partial class BuildTargetTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture : ServiceFixture
	{
		public string Repository { get; } = FindRepository();
		public string Sample { get; set; } = string.Empty;
		public Dictionary<string, string> Environment { get; } = [];

		public string Scratch()
		{
			var directory = Path.Combine(Path.GetTempPath(), $"fluentgwt-{TestId}-{Guid.NewGuid():N}");
			Directory.CreateDirectory(directory);
			OnTeardown(_ =>
			{
				Directory.Delete(directory, recursive: true);
				return ValueTask.CompletedTask;
			});
			return directory;
		}

		public async Task<string> Dotnet(string workingDirectory, string arguments, CancellationToken cancellationToken)
		{
			var start = new ProcessStartInfo("dotnet", arguments)
			{
				WorkingDirectory = workingDirectory,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
			start.Environment["DOTNET_NOLOGO"] = "1";
			foreach (var (name, value) in Environment)
				start.Environment[name] = value;
			using var process = Process.Start(start) ?? throw new InvalidOperationException($"dotnet {arguments} did not start.");
			var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
			var error = process.StandardError.ReadToEndAsync(cancellationToken);
			await process.WaitForExitAsync(cancellationToken);
			var all = $"{await output}{await error}";
			return process.ExitCode == 0
				? all
				: throw new InvalidOperationException($"dotnet {arguments} exited {process.ExitCode} in {workingDirectory}:{System.Environment.NewLine}{all}");
		}

		private static string FindRepository()
		{
			var directory = new DirectoryInfo(AppContext.BaseDirectory);
			while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FluentGwt.slnx")))
				directory = directory.Parent;
			return directory?.FullName ?? throw new InvalidOperationException("FluentGwt.slnx was not found above the test output.");
		}
	}
}
