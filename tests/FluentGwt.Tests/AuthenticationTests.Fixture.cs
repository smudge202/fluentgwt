using System.Net;
using FluentGwt.Tests.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FluentGwt.Tests;

public sealed partial class AuthenticationTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture : ServiceFixture
	{
		public Fixture() => Api = ApplicationHost.For<Program>(this);

		public ApplicationHost Api { get; }
		public string Tenant { get; set; } = "declared";
		public Backchannel Backchannel { get; } = new();

		protected override ValueTask DisposeFixture()
		{
			Backchannel.Dispose();
			return ValueTask.CompletedTask;
		}
	}

	internal sealed class BareFixture : ServiceFixture
	{
		public BareFixture() => Bare = ApplicationHost.Composed(
			this,
			"bare",
			builder => builder.Services.AddAuthorization(),
			app => app.MapGet("/me", (HttpContext context) => context.User.Identity?.AuthenticationType ?? string.Empty).RequireAuthorization());

		public ApplicationHost Bare { get; }
	}

	internal sealed class Backchannel : HttpMessageHandler
	{
		private int _calls;

		public int Calls => _calls;

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref _calls);
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
		}
	}

	internal sealed record Reply(HttpStatusCode Status, string Body, bool StampedByApplication);

	internal static IEnumerable<string> FailedRequirementTypes(AuthorisationDecision decision) =>
		decision.FailedRequirements.Select(x => x.GetType().Name);

	internal static bool IsClaimsRequirement(IAuthorizationRequirement requirement) =>
		requirement is Microsoft.AspNetCore.Authorization.Infrastructure.ClaimsAuthorizationRequirement;
}
