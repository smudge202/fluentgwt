using System.Reflection;

namespace FluentGwt;

public static class IntegrationGate
{
	public static bool IsOpen => Assembly.GetEntryAssembly()?.IsDefined(typeof(IntegrationEnabledAttribute), inherit: false) ?? false;

	internal static string SkipMessage(IntegrationJustification justification, string reason)
	{
		if (justification == IntegrationJustification.None)
			throw new ArgumentException("An integration test must declare its justification: what makes it one.", nameof(justification));
		if (string.IsNullOrWhiteSpace(reason))
			throw new ArgumentException("An integration test must give a reason: why, in the author's words.", nameof(reason));
		return $"Integration disabled (built without FluentGwtIntegration=true). Justification: {justification}. Reason: {reason}";
	}
}
