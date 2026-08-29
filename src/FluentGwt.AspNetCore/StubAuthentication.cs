using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentGwt;

internal static class StubAuthentication
{
	public static void Apply(IServiceCollection services, TestAuthentication authentication, TestAuthorisation authorisation)
	{
		var replaced = new ConcurrentDictionary<string, Type>();
		services.AddSingleton(authentication);
		services.AddSingleton(provider => new SchemeClaimTypes(provider, replaced));
		services.AddAuthentication();
		services.PostConfigure<AuthenticationOptions>(options =>
		{
			foreach (var scheme in options.Schemes)
			{
				if (scheme.HandlerType is { } original && original != typeof(StubAuthenticationHandler))
					replaced[scheme.Name] = original;
				scheme.HandlerType = typeof(StubAuthenticationHandler);
			}
			if (options.Schemes.Any())
				return;
			options.AddScheme<StubAuthenticationHandler>(TestAuthentication.DefaultScheme, null);
			options.DefaultScheme = TestAuthentication.DefaultScheme;
		});
		var existing = services.LastOrDefault(x => x.ServiceType == typeof(IAuthorizationMiddlewareResultHandler) && !x.IsKeyedService);
		services.RemoveAll<IAuthorizationMiddlewareResultHandler>();
		services.AddSingleton<IAuthorizationMiddlewareResultHandler>(provider => new RecordingResultHandler(Inner(provider, existing), authorisation));
	}

	private static IAuthorizationMiddlewareResultHandler Inner(IServiceProvider provider, ServiceDescriptor? existing) =>
		existing switch
		{
			{ ImplementationInstance: IAuthorizationMiddlewareResultHandler instance } => instance,
			{ ImplementationType: { } type } => (IAuthorizationMiddlewareResultHandler)ActivatorUtilities.CreateInstance(provider, type),
			{ ImplementationFactory: { } factory } => (IAuthorizationMiddlewareResultHandler)factory(provider),
			_ => new AuthorizationMiddlewareResultHandler(),
		};

	[SuppressMessage("Performance", "CA1812", Justification = "Instantiated by the authentication handler provider, from the scheme's handler type.")]
	private sealed class StubAuthenticationHandler(
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		UrlEncoder encoder,
		TestAuthentication authentication,
		SchemeClaimTypes claimTypes)
		: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
	{
		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			var (nameType, roleType) = claimTypes.For(Scheme.Name);
			var principal = authentication.Authenticate(Scheme.Name, nameType, roleType, Request.Path.Value ?? string.Empty);
			return Task.FromResult(principal is null
				? AuthenticateResult.NoResult()
				: AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
		}
	}

	private sealed class SchemeClaimTypes(IServiceProvider provider, ConcurrentDictionary<string, Type> replaced)
	{
		private readonly ConcurrentDictionary<string, (string Name, string Role)> _types = new();

		public (string Name, string Role) For(string scheme) => _types.GetOrAdd(scheme, Discover);

		// The replaced scheme's claim types live on its own options type, which this package does not reference
		// (a bearer or OpenID Connect scheme's TokenValidationParameters), so they are read by reflection, and
		// any scheme that declares none keeps ClaimsIdentity's defaults.
		private (string Name, string Role) Discover(string scheme)
		{
			var defaults = (ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
			if (!replaced.TryGetValue(scheme, out var handler) || OptionsType(handler) is not { } optionsType)
				return defaults;
			var monitorType = typeof(IOptionsMonitor<>).MakeGenericType(optionsType);
			var options = monitorType.GetMethod(nameof(IOptionsMonitor<object>.Get))?.Invoke(provider.GetService(monitorType), [scheme]);
			var parameters = options?.GetType().GetProperty("TokenValidationParameters")?.GetValue(options);
			var name = parameters?.GetType().GetProperty("NameClaimType")?.GetValue(parameters) as string;
			var role = parameters?.GetType().GetProperty("RoleClaimType")?.GetValue(parameters) as string;
			return (name ?? defaults.DefaultNameClaimType, role ?? defaults.DefaultRoleClaimType);
		}

		private static Type? OptionsType(Type handler)
		{
			for (var type = handler; type is not null; type = type.BaseType)
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AuthenticationHandler<>))
					return type.GetGenericArguments()[0];
			return null;
		}
	}

	private sealed class RecordingResultHandler(IAuthorizationMiddlewareResultHandler inner, TestAuthorisation authorisation)
		: IAuthorizationMiddlewareResultHandler
	{
		public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
		{
			var endpoint = context.GetEndpoint();
			authorisation.Record(new(
				endpoint?.DisplayName ?? context.Request.Path.Value ?? string.Empty,
				[.. endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(x => x.Policy).OfType<string>() ?? []],
				authorizeResult.Succeeded,
				[.. authorizeResult.AuthorizationFailure?.FailedRequirements ?? []]));
			return inner.HandleAsync(next, context, policy, authorizeResult);
		}
	}
}
