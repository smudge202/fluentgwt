using System.Globalization;
using System.Net;
using static FluentGwt.Tests.HostRedirectionTests;

namespace FluentGwt.Tests;

internal static class HostRedirectionTestsFluent
{
	public static When<Fixture, HttpStatusCode> WhenPublishing(this Given<Fixture> given, string entry)
		=> given.When(async (x, cancellationToken) =>
		{
			using var client = x.Publisher.CreateClient();
			using var response = await client.PostAsync(new Uri($"/publish/{entry}", UriKind.Relative), null, cancellationToken);
			var relayed = int.Parse(await response.Content.ReadAsStringAsync(cancellationToken), CultureInfo.InvariantCulture);
			return response.IsSuccessStatusCode ? (HttpStatusCode)relayed : response.StatusCode;
		});
}
