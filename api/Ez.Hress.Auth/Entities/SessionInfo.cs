namespace Ez.Hress.Auth.Entities;

/// <summary>
/// Settings for the auth app's own login session (the cookie that gives single sign-on across apps).
/// The session is a JWT signed with <see cref="Key"/>, which is separate from the access-token key.
/// </summary>
public class SessionInfo
{
    public const string CookieName = "__Host-hress-session";
    public const string Audience = "hress-auth-session";

    public SessionInfo(string key, string issuer, TimeSpan lifetime)
    {
        Key = key;
        Issuer = issuer;
        Lifetime = lifetime;
    }

    public string Key { get; set; }
    public string Issuer { get; set; }
    public TimeSpan Lifetime { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key) || Key.Length < 32)
            throw new ArgumentException("Session key must be at least 32 characters.", nameof(Key));

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new ArgumentNullException(nameof(Issuer));

        if (Lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(Lifetime));
    }
}
