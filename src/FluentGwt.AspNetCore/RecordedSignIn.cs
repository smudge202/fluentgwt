using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace FluentGwt;

public sealed record RecordedSignIn(string Scheme, ClaimsPrincipal Principal, AuthenticationProperties? Properties, string Path);
