using Microsoft.AspNetCore.Authentication;

namespace FluentGwt;

public sealed record RecordedSignOut(string Scheme, AuthenticationProperties? Properties, string Path);
