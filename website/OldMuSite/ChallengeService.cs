namespace OldMu.Site;

using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

/// <summary>
/// Issues and checks the proof-of-work challenges of the registration form.
/// </summary>
/// <remarks>
/// A challenge is a random salt, its issue time and the difficulty, signed with a key which only
/// lives in this process, so nothing has to be stored until a challenge is used. The browser has
/// to find a nonce for which SHA-256("salt:nonce") starts with the required number of zero bits.
/// That costs a visitor a second or two once, but makes mass registration expensive. A challenge
/// can be used only once, and only between <see cref="SiteOptions.MinimumFormSeconds"/> and
/// <see cref="MaximumAge"/> after it was issued.
/// </remarks>
public sealed class ChallengeService
{
    private static readonly TimeSpan MaximumAge = TimeSpan.FromMinutes(20);

    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    private readonly ConcurrentDictionary<string, DateTime> _usedSalts = new();
    private readonly IOptionsMonitor<SiteOptions> _options;

    /// <summary>Initializes a new instance of the <see cref="ChallengeService"/> class.</summary>
    /// <param name="options">The site options.</param>
    public ChallengeService(IOptionsMonitor<SiteOptions> options) => this._options = options;

    /// <summary>Issues a new challenge.</summary>
    /// <returns>The signed token and the number of zero bits to find.</returns>
    public (string Token, int Bits) Issue()
    {
        var bits = Math.Clamp(this._options.CurrentValue.ProofOfWorkBits, 1, 30);
        var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var payload = string.Create(CultureInfo.InvariantCulture, $"{salt}.{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.{bits}");
        return (payload + "." + this.Sign(payload), bits);
    }

    /// <summary>Checks a solved challenge and marks it as used.</summary>
    /// <param name="token">The token from <see cref="Issue"/>.</param>
    /// <param name="nonce">The nonce which the browser found.</param>
    /// <returns><c>null</c> when it's valid; otherwise the reason for the visitor.</returns>
    public string? Verify(string? token, string? nonce)
    {
        var parts = (token ?? string.Empty).Split('.');
        if (parts.Length != 4 || string.IsNullOrEmpty(nonce) || nonce.Length > 32)
        {
            return "The security check is missing. Reload the page and try again.";
        }

        var payload = $"{parts[0]}.{parts[1]}.{parts[2]}";
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(this.Sign(payload)), Encoding.ASCII.GetBytes(parts[3]))
            || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var issuedMs)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var bits))
        {
            return "The security check is invalid. Reload the page and try again.";
        }

        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(issuedMs);
        if (age < TimeSpan.FromSeconds(this._options.CurrentValue.MinimumFormSeconds))
        {
            return "That was too fast. Wait a moment and try again.";
        }

        if (age > MaximumAge)
        {
            return "The security check has expired. Try again.";
        }

        if (!HasLeadingZeroBits(SHA256.HashData(Encoding.ASCII.GetBytes(parts[0] + ":" + nonce)), bits))
        {
            return "The security check failed. Try again.";
        }

        this.RemoveExpiredSalts();
        if (!this._usedSalts.TryAdd(parts[0], DateTime.UtcNow))
        {
            return "The security check was already used. Try again.";
        }

        return null;
    }

    private static bool HasLeadingZeroBits(byte[] hash, int bits)
    {
        var index = 0;
        for (; bits >= 8; bits -= 8, index++)
        {
            if (hash[index] != 0)
            {
                return false;
            }
        }

        return bits == 0 || (hash[index] >> (8 - bits)) == 0;
    }

    private string Sign(string payload)
        => Convert.ToHexString(HMACSHA256.HashData(this._key, Encoding.ASCII.GetBytes(payload))).ToLowerInvariant();

    private void RemoveExpiredSalts()
    {
        var limit = DateTime.UtcNow - MaximumAge;
        foreach (var (salt, used) in this._usedSalts)
        {
            if (used < limit)
            {
                this._usedSalts.TryRemove(salt, out _);
            }
        }
    }
}
