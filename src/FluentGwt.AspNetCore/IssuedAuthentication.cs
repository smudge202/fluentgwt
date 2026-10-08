using System.Security.Claims;

namespace FluentGwt;

public sealed record IssuedAuthentication(string Scheme, ClaimsPrincipal Principal, string Path);
