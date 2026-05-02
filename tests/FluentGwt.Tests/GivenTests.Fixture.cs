namespace FluentGwt.Tests;

public sealed partial class GivenTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture
	{
		public Subject Subject { get; } = new();
		public object State { get; } = new();
		public Part First { get; } = new();
		public Part Second { get; } = new();
		public string Name { get; } = "state-name";
		public object Key { get; } = new();
		public int Value { get; } = 42;
	}

	internal sealed class Subject
	{
		public Part? Part { get; set; }

		public static async Task<Part> NewPartLater()
		{
			await Task.Yield();
			return new();
		}

		public static async Task<int> ValueLater(int value)
		{
			await Task.Yield();
			return value;
		}
	}

	internal sealed class Part
	{
		public int? Value { get; set; }
	}
}
