using System.Runtime.CompilerServices;
using Xunit;

namespace FluentGwt;

public sealed class IntegrationFactAttribute : FactAttribute
{
	public IntegrationFactAttribute(
		IntegrationJustification justification,
		string reason,
		[CallerFilePath] string? sourceFilePath = null,
		[CallerLineNumber] int sourceLineNumber = -1)
		: base(sourceFilePath, sourceLineNumber)
	{
		Skip = IntegrationGate.SkipMessage(justification, reason);
		SkipUnless = nameof(IntegrationGate.IsOpen);
		SkipType = typeof(IntegrationGate);
		Justification = justification;
		Reason = reason;
	}

	public IntegrationJustification Justification { get; }

	public string Reason { get; }
}
