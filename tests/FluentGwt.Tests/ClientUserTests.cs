using System.Net;
using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ClientUserTests
{
	[Fact]
	public Task WhenTwoClientsAreBoundToTwoPeopleThenEachRequestIsMadeAsItsOwnPerson()
		=> Context
			.GivenStubbedAuthentication()
			.WhenAliceAndBobEachGet("/whoami")
			.Then(seen => seen.Should().Be(("alice", "bob")));

	[Fact]
	public Task WhenTwoClientsAreBoundThenEachUserHasItsOwnNameIdentifier()
		=> Context
			.GivenStubbedAuthentication()
			.WhenAliceAndBobEachGet("/id")
			.Then(ids => ids.Alice.Should().NotBe(ids.Bob));

	[Fact]
	public Task WhenAClientIsCreatedWithoutAUserThenItUsesTheHostUser()
		=> Context
			.GivenStubbedAuthentication()
			.Given(x => x.Api.Authentication().Claim(Fixture.User, "host"))
			.When(async (x, cancellationToken) =>
			{
				using var alice = x.Api.CreateClient(user => user.Claim(Fixture.User, "alice"));
				using var anyone = x.Api.CreateClient();
				return (
					await alice.GetStringAsync(new Uri("/whoami", UriKind.Relative), cancellationToken),
					await anyone.GetStringAsync(new Uri("/whoami", UriKind.Relative), cancellationToken));
			})
			.Then(seen => seen.Should().Be(("alice", "host")));

	[Fact]
	public Task WhenABoundUserHasARoleThenRoleAuthorisationSeesIt()
		=> Context
			.GivenStubbedAuthentication()
			.When(async (x, cancellationToken) =>
			{
				using var reviewer = x.Api.CreateClient(user => user.Claim(Fixture.User, "rita").Role(Fixture.Reviewer));
				using var author = x.Api.CreateClient(user => user.Claim(Fixture.User, "arthur"));
				using var reviewed = await reviewer.GetAsync(new Uri("/review", UriKind.Relative), cancellationToken);
				using var refused = await author.GetAsync(new Uri("/review", UriKind.Relative), cancellationToken);
				return (reviewed.StatusCode, refused.StatusCode);
			})
			.Then(statuses => statuses.Should().Be((HttpStatusCode.OK, HttpStatusCode.Forbidden)));

	[Fact]
	public Task WhenABoundClientMakesARequestThenTheApplicationNeverSeesTheUserHeader()
		=> Context
			.GivenStubbedAuthentication()
			.When(async (x, cancellationToken) =>
			{
				using var alice = x.Api.CreateClient(user => user.Claim(Fixture.User, "alice"));
				return await alice.GetStringAsync(new Uri("/headers", UriKind.Relative), cancellationToken);
			})
			.Then(headers => headers.Should().BeEmpty());

	[Fact]
	public Task WhenABoundClientIsAuthenticatedThenIssuedRecordsItsPrincipal()
		=> Context
			.GivenStubbedAuthentication()
			.WhenAliceAndBobEachGet("/whoami")
			.ThenFixture(x => x.Api.Authentication().Issued.Select(i => i.Principal.FindFirst(Fixture.User)?.Value)
				.Should().Contain(["alice", "bob"]));

	[Fact]
	public Task WhenABoundClientIsCreatedWithoutStubbedAuthenticationThenItFailsNamingTheFix()
		=> Context
			.Given()
			.When(x => x.Api.CreateClient(user => user.Claim(Fixture.User, "alice")))
			.ThenThrows<InvalidOperationException>(e => e.Message.Should().Contain("Authentication()"));
}
