namespace OldMu.Site;

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Options;

/// <summary>
/// Limits how many accounts one address, and all visitors together, may create.
/// </summary>
/// <remarks>
/// The created accounts of the last 24 hours are kept in <c>registrations.json</c> in the data
/// folder, so a restart doesn't reset the limits. IPv6 addresses are counted per /64 network,
/// because one connection usually gets a whole /64 and could change its address at will.
/// </remarks>
public sealed class RegistrationLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromHours(24);

    private readonly object _lock = new();
    private readonly List<Entry> _entries;
    private readonly string _filePath;
    private readonly IOptionsMonitor<SiteOptions> _options;
    private readonly ILogger<RegistrationLimiter> _logger;

    /// <summary>Initializes a new instance of the <see cref="RegistrationLimiter"/> class.</summary>
    /// <param name="options">The site options.</param>
    /// <param name="logger">The logger.</param>
    public RegistrationLimiter(IOptionsMonitor<SiteOptions> options, ILogger<RegistrationLimiter> logger)
    {
        this._options = options;
        this._logger = logger;
        var directory = Path.GetFullPath(options.CurrentValue.DataDirectory);
        Directory.CreateDirectory(directory);
        this._filePath = Path.Combine(directory, "registrations.json");
        this._entries = this.Load();
    }

    /// <summary>Normalizes an address to the key which is counted.</summary>
    /// <param name="address">The client address.</param>
    /// <returns>The key: the IPv4 address or the IPv6 /64 network.</returns>
    public static string GetKey(IPAddress? address)
    {
        if (address is null)
        {
            return "unknown";
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes) + "/64";
    }

    /// <summary>Reserves one registration for the address, if the limits allow it.</summary>
    /// <param name="key">The key of <see cref="GetKey"/>.</param>
    /// <param name="reservation">The reservation, which is released again when the account wasn't created.</param>
    /// <returns><c>null</c> when reserved; otherwise the reason for the visitor.</returns>
    public string? TryReserve(string key, out object? reservation)
    {
        reservation = null;
        var options = this._options.CurrentValue;
        var now = DateTime.UtcNow;
        lock (this._lock)
        {
            this._entries.RemoveAll(e => e.Utc < now - Window);
            if (this._entries.Count(e => e.Utc >= now.AddHours(-1)) >= options.AccountsPerHourTotal)
            {
                this._logger.LogWarning("Registration refused for {Key}: total limit of {Limit} accounts per hour reached.", key, options.AccountsPerHourTotal);
                return "Too many accounts were created in the last hour. Please try again later.";
            }

            var own = this._entries.Where(e => e.Key == key).ToList();
            if (own.Count >= options.AccountsPerIpPerDay)
            {
                var retry = own.Min(e => e.Utc) + Window - now;
                return $"You have already created {own.Count} accounts today. Try again in {FormatDuration(retry)}.";
            }

            var last = own.Count > 0 ? own.Max(e => e.Utc) : DateTime.MinValue;
            var wait = last.AddMinutes(options.MinutesBetweenAccountsPerIp) - now;
            if (wait > TimeSpan.Zero)
            {
                return $"You have just created an account. Try again in {FormatDuration(wait)}.";
            }

            var entry = new Entry(key, now);
            this._entries.Add(entry);
            this.Save();
            reservation = entry;
            return null;
        }
    }

    /// <summary>Releases a reservation, because the account wasn't created.</summary>
    /// <param name="reservation">The reservation of <see cref="TryReserve"/>.</param>
    public void Release(object? reservation)
    {
        if (reservation is not Entry entry)
        {
            return;
        }

        lock (this._lock)
        {
            if (this._entries.Remove(entry))
            {
                this.Save();
            }
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMinutes < 1)
        {
            return "a minute";
        }

        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours} h {duration.Minutes} min"
            : $"{(int)Math.Ceiling(duration.TotalMinutes)} min";
    }

    private List<Entry> Load()
    {
        try
        {
            if (File.Exists(this._filePath))
            {
                return JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(this._filePath)) ?? [];
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Could not read {File}; starting with empty limits.", this._filePath);
        }

        return [];
    }

    private void Save()
    {
        try
        {
            var temp = this._filePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(this._entries));
            File.Move(temp, this._filePath, true);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Could not write {File}.", this._filePath);
        }
    }

    /// <summary>One created account.</summary>
    /// <param name="Key">The address key.</param>
    /// <param name="Utc">When it was created.</param>
    private sealed record Entry(string Key, DateTime Utc);
}
