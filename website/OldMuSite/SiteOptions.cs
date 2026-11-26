namespace OldMu.Site;

/// <summary>
/// Settings of the website, from the <c>Site</c> section (environment: <c>Site__ApiKey</c>, ...).
/// </summary>
public sealed class SiteOptions
{
    /// <summary>Base address of the game server's admin panel API, e.g. <c>http://openmu-startup:8080</c>.</summary>
    public string ApiUrl { get; set; } = "http://localhost:8080";

    /// <summary>API key with the operator role, created in the admin panel (API keys page).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Where the "Download" button points to.</summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>Where the "Discord" button points to; hidden when empty.</summary>
    public string DiscordUrl { get; set; } = string.Empty;

    /// <summary>Folder for the registration log which keeps the per-IP limits across restarts.</summary>
    public string DataDirectory { get; set; } = "data";

    /// <summary>
    /// Leading zero bits of the proof of work which the browser solves before it may register.
    /// Every bit doubles the work: 21 takes a PC about a second and a phone a few seconds.
    /// </summary>
    public int ProofOfWorkBits { get; set; } = 21;

    /// <summary>A form sent sooner than this after the page got its challenge is refused.</summary>
    public int MinimumFormSeconds { get; set; } = 3;

    /// <summary>Accounts which one IP address (an IPv6 /64 network) may create per 24 hours.</summary>
    public int AccountsPerIpPerDay { get; set; } = 3;

    /// <summary>Minutes which one IP address has to wait between two new accounts.</summary>
    public int MinutesBetweenAccountsPerIp { get; set; } = 10;

    /// <summary>Accounts which all visitors together may create per hour; stops a flood through many proxies.</summary>
    public int AccountsPerHourTotal { get; set; } = 40;

    /// <summary>
    /// Whether the client address is taken from <c>X-Forwarded-For</c>. Only switch it on when the
    /// site runs behind a reverse proxy (Caddy, nginx), otherwise visitors could fake their address.
    /// </summary>
    public bool TrustForwardedHeaders { get; set; }
}
