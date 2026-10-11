using System.Net;
using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class KeptSchemeTests
{
	[Fact]
	public Task WhenNoSchemeIsKeptThenEverySchemeIsStubbed()
		=> Context
			.GivenAStubbedUserKeeping()
			.WhenGetting("/machine")
			.Then(reply => reply.Should().Be(new Reply(HttpStatusCode.OK, ApiKeyHandler.Name)));

	[Fact]
	public Task WhenSchemeIsKeptThenItsOwnHandlerAcceptsARealCredential()
		=> Context
			.GivenAStubbedUserKeeping(ApiKeyHandler.Name)
			.WhenGetting("/machine", ApiKeyHandler.Right)
			.Then(reply => reply.Should().Be(new Reply(HttpStatusCode.OK, ApiKeyHandler.Name)))
			.AndFixture(x => x.Api.Authentication().Issued.Should().NotContain(issued => issued.Scheme == ApiKeyHandler.Name));

	[Fact]
	public Task WhenSchemeIsKeptThenItsOwnHandlerRefusesAWrongCredential()
		=> Context
			.GivenAStubbedUserKeeping(ApiKeyHandler.Name)
			.WhenGetting("/machine", "wrong")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.Unauthorized));

	[Fact]
	public Task WhenSchemeIsKeptThenOtherSchemesAreStillStubbed()
		=> Context
			.GivenAStubbedUserKeeping(ApiKeyHandler.Name)
			.WhenGetting("/human")
			.Then(reply => reply.Should().Be(new Reply(HttpStatusCode.OK, Fixture.Cookies)));

	[Fact]
	public Task WhenPolicySchemeIsKeptThenACredentialIsForwardedToTheKeptScheme()
		=> Context
			.GivenAStubbedUserKeeping(ApiKeyHandler.Name, Fixture.Either)
			.WhenGetting("/either", ApiKeyHandler.Right)
			.Then(reply => reply.Should().Be(new Reply(HttpStatusCode.OK, ApiKeyHandler.Name)));

	[Fact]
	public Task WhenPolicySchemeIsKeptThenARequestWithoutACredentialIsForwardedToTheStub()
		=> Context
			.GivenAStubbedUserKeeping(ApiKeyHandler.Name, Fixture.Either)
			.WhenGetting("/either")
			.Then(reply => reply.Should().Be(new Reply(HttpStatusCode.OK, Fixture.Cookies)));

	[Fact]
	public Task WhenKeptSchemeIsNotRegisteredThenArrangementFailsNamingTheSchemes()
		=> Context
			.GivenAStubbedUserKeeping("Missing")
			.WhenGetting("/machine")
			.ThenArrangementFails<Exception>(e => e.ToString().Should().Contain("Keep(\"Missing\")").And.Contain("ApiKey, Cookies, Either"));
}
