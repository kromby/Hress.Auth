namespace Ez.Hress.Auth.Entities;

/// <summary>
/// Origins that may call the auth API from a browser on another site (e.g. Hress.Org calling POST /api/token).
/// Origins are matched exactly (scheme + host + port), never by prefix or wildcard.
/// </summary>
public class CorsPolicy
{
    public CorsPolicy(IEnumerable<string> allowedOrigins)
    {
        AllowedOrigins = allowedOrigins
            .Select(o => o.Trim().TrimEnd('/'))
            .Where(o => o.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlySet<string> AllowedOrigins { get; }

    public static CorsPolicy FromSetting(string? commaSeparatedOrigins) =>
        new((commaSeparatedOrigins ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries));

    public bool IsAllowed(string? origin) =>
        !string.IsNullOrWhiteSpace(origin) && AllowedOrigins.Contains(origin.Trim());
}
