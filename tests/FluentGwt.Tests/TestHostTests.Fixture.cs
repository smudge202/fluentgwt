using System.Net;
using System.Net.Http.Headers;
using FluentGwt.Tests.Web;

namespace FluentGwt.Tests;

public sealed partial class TestHostTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture : ServiceFixture
	{
		public Fixture()
		{
			Api = ApplicationHost.For<Program>(this);
			Composed = ApplicationHost.Composed(this, "composed", builder => builder.Services.AddWeb(), app => app.MapWeb());
		}

		public ApplicationHost Api { get; }
		public ApplicationHost Composed { get; }
		public Journal Journal { get; } = new();
		public string? Seen { get; set; }
	}

	internal sealed record Reply(HttpStatusCode Status, string Body, HttpResponseHeaders Headers);

	internal sealed class ShoutingGreeter : Greeter
	{
		public string Greet() => "HELLO";
	}
}
