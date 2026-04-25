namespace FluentGwt.Tests;

internal sealed class Probe
{
	public int Acts { get; private set; }
	public object Result { get; } = new();
	public Exception Failure { get; } = new InvalidOperationException("Probe failure");
	public OperationCanceledException Cancellation { get; } = new();
	public List<string> Events { get; } = [];
	public string Log => string.Join(',', Events);

	public void Act() => Acts++;

	public void Record(string step) => Events.Add(step);

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

	public object AnswerOrFail() => Acts < 0 ? Result : throw Failure;

	public void Cancel() => throw Cancellation;
}
