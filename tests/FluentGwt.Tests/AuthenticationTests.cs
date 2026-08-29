using System.Net;
using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class AuthenticationTests
{
	[Fact]
	public Task WhenNoUserIsGivenThenProtectedEndpointIsUnauthorised()
		=> Context
			.GivenStubbedAuthentication()
			.WhenGetting("/me")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.Unauthorized));

	[Fact]
	public Task WhenAuthenticatedUserIsGivenThenRequestIsAuthenticated()
		=> Context
			.GivenStubbedAuthentication()
			.GivenAuthenticatedUser()
			.WhenGetting("/me")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.OK));

	[Fact]
	public Task WhenAuthenticatedUserIsGivenThenTheirNameIdentifierComesFromTheTest()
		=> Context
			.GivenStubbedAuthentication()
			.GivenAuthenticatedUser()
			.WhenGetting("/me")
			.Then((x, reply) => reply.Body.Should().Contain($"user-{x.TestId}"));

	[Fact]
	public Task WhenClaimIsGivenThenPrincipalCarriesIt()
		=> Context
			.GivenStubbedAuthentication()
			.GivenClaim("tid", "tenant-1")
			.When(x => x.Api.Authentication().Principal)
			.Then(principal => principal.HasClaim("tid", "tenant-1").Should().BeTrue());

	[Fact]
	public Task WhenClaimIsGivenThenItReachesTheEndpointsUser()
		=> Context
			.GivenStubbedAuthentication()
			.GivenClaim("tid", "tenant-1")
			.WhenGetting("/me")
			.Then(reply => reply.Body.Should().Contain("tid=tenant-1"));

	[Fact]
	public Task WhenClaimValueComesFromFixtureThenItIsEvaluatedAtExecution()
		=> Context
			.GivenStubbedAuthentication()
			.GivenClaim("tid", x => x.Tenant)
			.Given(x => x.Tenant = "assigned later")
			.WhenGetting("/me")
			.Then(reply => reply.Body.Should().Contain("tid=assigned later"));

	[Fact]
	public Task WhenRequiredClaimIsMissingThenRequestIsForbidden()
		=> Context
			.GivenStubbedAuthentication()
			.GivenAuthenticatedUser()
			.WhenGetting("/read")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.Forbidden));

	[Fact]
	public Task WhenRequiredClaimIsGivenThenRequestIsAuthorised()
		=> Context
			.GivenStubbedAuthentication()
			.GivenClaim("scope", "read")
			.WhenGetting("/read")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.OK));

	[Fact]
	public Task WhenPolicyIsOverriddenThenNewRequirementsApply()
		=> Context
			.GivenStubbedAuthentication()
			.GivenAuthenticatedUser()
			.GivenPolicy("Reader", policy => policy.RequireAuthenticatedUser())
			.WhenGetting("/read")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.OK));

	[Fact]
	public Task WhenAnonymousUserIsGivenAfterClaimsThenRequestIsUnauthorised()
		=> Context
			.GivenStubbedAuthentication()
			.GivenClaim("tid", "tenant-1")
			.GivenAnonymousUser()
			.WhenGetting("/me")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.Unauthorized));

	[Fact]
	public Task WhenTestsRunInParallelThenPrincipalsAreIsolated()
		=> Context
			.GivenStubbedAuthentication()
			.GivenClaim("tid", "tenant-1")
			.When(async (_, cancellationToken) =>
			{
				var other = new Fixture();
				var status = HttpStatusCode.OK;
				await other.GivenStubbedAuthentication().When(async (y, token) => status = (await AuthenticationTestsFluent.Get(y.Api, "/me", null, token)).Status).Then(_ => { });
				return status;
			})
			.Then(status => status.Should().Be(HttpStatusCode.Unauthorized));

	[Fact]
	public Task WhenApplicationRegistersEntraIdThenItsSchemeIsStubbedUnderTheSameName()
		=> Context
			.GivenStubbedAuthentication()
			.GivenAuthenticatedUser()
			.WhenGetting("/me")
			.Then((x, _) => x.Api.Authentication().Issued.Should().ContainSingle().Which.Scheme.Should().Be("Bearer"));

	[Fact]
	public Task WhenSchemesAreStubbedThenNoRequestReachesTheIdentityProvider()
		=> Context
			.GivenStubbedAuthentication()
			.GivenTheIdentityProviderIsWatched()
			.GivenAuthenticatedUser()
			.WhenGetting("/me", bearer: "a.token.the-real-handler-would-validate")
			.Then((x, reply) => (reply.Status, x.Backchannel.Calls).Should().Be((HttpStatusCode.OK, 0)));

	[Fact]
	public Task WhenApplicationRegistersNoSchemeThenStubSchemeIsTheDefault()
		=> Context
			.Given()
			.When(async (_, cancellationToken) =>
			{
				var bare = new BareFixture();
				var body = string.Empty;
				await bare
					.Given(y => y.Bare.Authentication().AuthenticateAs())
					.When(async (y, token) => body = (await AuthenticationTestsFluent.Get(y.Bare, "/me", null, token)).Body)
					.Then(_ => { });
				return body;
			})
			.Then(body => body.Should().Be("FluentGwt"));

	[Fact]
	public Task WhenStubbedSchemeChallengesThenResponseIsUnauthorisedNotARedirect()
		=> Context
			.GivenStubbedAuthentication()
			.WhenGetting("/cookie")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.Unauthorized));

	[Fact]
	public Task WhenUserIsGivenForOneSchemeThenOtherSchemesStayAnonymous()
		=> Context
			.GivenStubbedAuthentication()
			.GivenAuthenticatedUser("Partner")
			.When(async (x, cancellationToken) => (await AuthenticationTestsFluent.Get(x.Api, "/partner", null, cancellationToken), await AuthenticationTestsFluent.Get(x.Api, "/me", null, cancellationToken)))
			.Then(replies => (replies.Item1.Status, replies.Item2.Status).Should().Be((HttpStatusCode.OK, HttpStatusCode.Unauthorized)));

	[Fact]
	public Task WhenSchemeDeclaresRoleClaimTypeThenGivenRoleSatisfiesRoleAuthorisation()
		=> Context
			.GivenStubbedAuthentication()
			.GivenRole("Admin")
			.WhenGetting("/admin")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.OK))
			.AndFixture(x => x.Api.Authentication().Issued.Should().ContainSingle().Which.Principal.HasClaim("roles", "Admin").Should().BeTrue());

	[Fact]
	public Task WhenRequestIsAuthenticatedThenIssuedRecordsSchemeAndPrincipal()
		=> Context
			.GivenStubbedAuthentication()
			.GivenClaim("tid", "tenant-1")
			.WhenGetting("/me")
			.Then((x, _) => x.Api.Authentication().Issued.Should().ContainSingle()
				.Which.Should().Match<IssuedAuthentication>(i => i.Scheme == "Bearer" && i.Principal.HasClaim("tid", "tenant-1") && i.Path == "/me"));

	[Fact]
	public Task WhenPolicyIsEvaluatedThenDecisionRecordsOutcomeAndFailedRequirements()
		=> Context
			.GivenStubbedAuthentication()
			.GivenAuthenticatedUser()
			.WhenGetting("/read")
			.Then((x, _) => x.Api.Authorisation.Decisions.Should().ContainSingle()
				.Which.Should().Match<AuthorisationDecision>(d => d.Policies.Contains("Reader") && !d.Succeeded && d.FailedRequirements.All(IsClaimsRequirement) && d.FailedRequirements.Count == 1));

	[Fact]
	public Task WhenDecisionsAreRecordedThenApplicationResultHandlerStillRuns()
		=> Context
			.GivenStubbedAuthentication()
			.GivenAuthenticatedUser()
			.WhenGetting("/me")
			.Then(reply => reply.StampedByApplication.Should().BeTrue());
}
