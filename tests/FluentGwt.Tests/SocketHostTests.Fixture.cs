using FluentGwt.Tests.Web;

namespace FluentGwt.Tests;

public sealed partial class SocketHostTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture : ServiceFixture
	{
		public Fixture()
		{
			Relay = ApplicationHost.For<Program>(this, "relay").OnSockets();
			Other = ApplicationHost.For<Program>(this, "other").OnSockets();
			InMemory = ApplicationHost.For<Program>(this);
			Composed = ApplicationHost.Composed(this, "composed", builder => builder.Services.AddWeb(), app => app.MapWeb());
		}

		public ApplicationHost Relay { get; }
		public ApplicationHost Other { get; }
		public ApplicationHost InMemory { get; }
		public ApplicationHost Composed { get; }
	}
}
