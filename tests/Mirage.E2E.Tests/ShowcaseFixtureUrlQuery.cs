namespace Mirage.E2E.Tests;

internal static class ShowcaseFixtureUrlQuery
{
    public static Uri Append(Uri baseUrl, params (string Name, string Value)[] pairs)
    {
        var builder = new UriBuilder(baseUrl);
        var entries = new List<string>();

        var existingQuery = builder.Query;
        if (!string.IsNullOrWhiteSpace(existingQuery))
        {
            entries.AddRange(existingQuery.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries));
        }

        foreach (var (name, value) in pairs)
        {
            entries.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
        }

        builder.Query = string.Join("&", entries);
        return builder.Uri;
    }
}
