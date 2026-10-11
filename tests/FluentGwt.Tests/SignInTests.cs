using System.Net;
using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class SignInTests
{
	[Fact]
	public Task WhenAnEndpointSignsInThroughAStubbedSchemeThenItCompletes()
		=> Context
			.GivenStubbedAuthentication()
			.WhenGetting("/sign-in")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.OK));

	[Fact]
	public Task WhenAnEndpointSignsInThroughAStubbedSchemeThenTheSignInIsRecorded()
		=> Context
			.GivenStubbedAuthentication()
			.WhenGetting("/sign-in")
			.ThenFixture(x => x.Api.Authentication().SignIns.Should().ContainSingle()
				.Which.Should().Match<RecordedSignIn>(s =>
					s.Scheme == Fixture.Cookies
					&& s.Principal.HasClaim(Fixture.Subject, Fixture.SignedInSubject)
					&& s.Properties != null && s.Properties.RedirectUri == Fixture.ReturnTo
					&& s.Path == "/sign-in"));

	[Fact]
	public Task WhenAnEndpointSignsOutThroughAStubbedSchemeThenItCompletesAndIsRecorded()
		=> Context
			.GivenStubbedAuthentication()
			.WhenGetting("/sign-out")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.OK))
			.AndFixture(x => x.Api.Authentication().SignOuts.Should().ContainSingle()
				.Which.Should().Match<RecordedSignOut>(s =>
					s.Scheme == Fixture.Cookies
					&& s.Properties != null && s.Properties.RedirectUri == Fixture.ReturnTo
					&& s.Path == "/sign-out"));

	[Fact]
	public Task WhenAnEndpointSignsInThenTheStubbedUserIsUnchanged()
		=> Context
			.GivenStubbedAuthentication()
			.WhenGetting("/sign-in", "/whoami")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.Unauthorized));

	[Fact]
	public Task WhenAnEndpointSignsOutThenTheStubbedUserIsUnchanged()
		=> Context
			.GivenStubbedAuthentication()
			.Given(x => x.Api.Authentication().Claim(Fixture.Subject, "arranged"))
			.WhenGetting("/sign-out", "/whoami")
			.Then(reply => reply.Should().Be(new Reply(HttpStatusCode.OK, "arranged")));
}
