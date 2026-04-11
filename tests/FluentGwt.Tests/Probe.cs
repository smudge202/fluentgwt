namespace FluentGwt.Tests;

internal sealed class Probe
{
	public int Acts { get; private set; }
	public object Result { get; } = new();
	public Exception Failure { get; } = new InvalidOperationException("Probe failure");

	public void Act() => Acts++;

	public object Answer()
	{
		Acts++;
		return Result;
	}

	public async ValueTask ActLater()
	{
		await Task.Yield();
		Acts++;
	}

	public async ValueTask<object> AnswerLater()
	{
		await Task.Yield();
		return Answer();
	}

	public async Task<object> AnswerEventually()
	{
		await Task.Yield();
		return Answer();
	}

	public void Fail() => throw Failure;
}
