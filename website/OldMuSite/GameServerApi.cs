namespace OldMu.Site;

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

/// <summary>The result of an account creation.</summary>
public enum CreateAccountResult
{
    /// <summary>The account was created.</summary>
    Created,

    /// <summary>The login name is already taken.</summary>
    Taken,

    /// <summary>The server refused the login name or password.</summary>
    Invalid,

    /// <summary>The server couldn't be reached or refused the API key.</summary>
    Unavailable,
}

/// <summary>The state of the game server.</summary>
/// <param name="Online">Whether the server runs.</param>
/// <param name="Players">The number of logged in players.</param>
/// <param name="Names">The names of the online characters.</param>
public sealed record ServerStatus(bool Online, int Players, IReadOnlySet<string> Names)
{
    /// <summary>Gets the status of a server which can't be reached.</summary>
    public static ServerStatus Offline { get; } = new(false, 0, new HashSet<string>());
}

/// <summary>
/// Talks to the admin panel API of the game server: <c>POST api/accounts</c>, <c>GET api/status</c> and <c>GET api/rankings</c>.
/// </summary>
public sealed class GameServerApi
{
    private static readonly TimeSpan StatusCacheDuration = TimeSpan.FromSeconds(30);

    /// <summary>The rankings change slowly and their queries are expensive.</summary>
    private static readonly TimeSpan RankingCacheDuration = TimeSpan.FromMinutes(2);

    private static readonly ConcurrentDictionary<string, (DateTime Time, JsonArray? Entries)> RankingCache = new();

    /// <summary>The guide information only changes when the configuration does.</summary>
    private static readonly TimeSpan GuideCacheDuration = TimeSpan.FromMinutes(10);

    private static readonly ConcurrentDictionary<string, (DateTime Time, string? Json)> GuideCache = new();

    private readonly HttpClient _client;
    private readonly IOptionsMonitor<SiteOptions> _options;
    private readonly ILogger<GameServerApi> _logger;
    private static (DateTime Time, ServerStatus Status)? cachedStatus;

    /// <summary>Initializes a new instance of the <see cref="GameServerApi"/> class.</summary>
    /// <param name="client">The http client.</param>
    /// <param name="options">The site options.</param>
    /// <param name="logger">The logger.</param>
    public GameServerApi(HttpClient client, IOptionsMonitor<SiteOptions> options, ILogger<GameServerApi> logger)
    {
        this._client = client;
        this._options = options;
        this._logger = logger;
    }

    /// <summary>Creates an account.</summary>
    /// <param name="loginName">The login name.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    public async Task<CreateAccountResult> CreateAccountAsync(string loginName, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(this._options.CurrentValue.ApiKey))
        {
            this._logger.LogError("Site:ApiKey is not set, so no account can be created.");
            return CreateAccountResult.Unavailable;
        }

        try
        {
            using var request = this.CreateRequest(HttpMethod.Post, "api/accounts");
            // A string content has a known length; JsonContent would be sent chunked.
            request.Content = new StringContent(JsonSerializer.Serialize(new { loginName, password }), System.Text.Encoding.UTF8, "application/json");
            using var response = await this._client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                case HttpStatusCode.OK:
                    return CreateAccountResult.Created;
                case HttpStatusCode.Conflict:
                    return CreateAccountResult.Taken;
                case HttpStatusCode.BadRequest:
                    return CreateAccountResult.Invalid;
                default:
                    this._logger.LogError("Account creation failed with {Status}: {Body}", (int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                    return CreateAccountResult.Unavailable;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            this._logger.LogError(ex, "The game server API is not reachable.");
            return CreateAccountResult.Unavailable;
        }
    }

    /// <summary>Gets whether the server is online and who is logged in; cached for a while.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status, with the names of the online characters.</returns>
    public async Task<ServerStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        if (cachedStatus is { } cached && DateTime.UtcNow - cached.Time < StatusCacheDuration)
        {
            return cached.Status;
        }

        var status = ServerStatus.Offline;
        try
        {
            using var request = this.CreateRequest(HttpMethod.Get, "api/status");
            using var response = await this._client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                // The server serializes the object to a string first, so the body is a JSON string with JSON inside.
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                var root = document.RootElement;
                using var inner = root.ValueKind == JsonValueKind.String ? JsonDocument.Parse(root.GetString()!) : null;
                var data = inner?.RootElement ?? root;
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (data.TryGetProperty("playersList", out var list) && list.ValueKind == JsonValueKind.Array)
                {
                    foreach (var name in list.EnumerateArray())
                    {
                        if (name.GetString() is { Length: > 0 } text)
                        {
                            names.Add(text);
                        }
                    }
                }

                status = new ServerStatus(true, data.TryGetProperty("players", out var players) ? players.GetInt32() : names.Count, names);
            }
            else
            {
                // The API answers, so the server runs; the key may just lack the viewer role.
                status = ServerStatus.Offline with { Online = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden };
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            this._logger.LogDebug(ex, "Status request failed.");
        }

        cachedStatus = (DateTime.UtcNow, status);
        return status;
    }

    /// <summary>Gets a ranking of the server API (<c>api/rankings/...</c>); cached for a while.</summary>
    /// <param name="path">The path below <c>api/rankings/</c>, with its query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <c>entries</c> of the ranking; <c>null</c> if the server couldn't deliver it.</returns>
    public async Task<JsonArray?> GetRankingAsync(string path, CancellationToken cancellationToken)
    {
        if (RankingCache.TryGetValue(path, out var cached) && DateTime.UtcNow - cached.Time < RankingCacheDuration)
        {
            return cached.Entries;
        }

        try
        {
            using var request = this.CreateRequest(HttpMethod.Get, "api/rankings/" + path);
            using var response = await this._client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                this._logger.LogWarning("Ranking {Path} failed with {Status}.", path, (int)response.StatusCode);
                return cached.Entries;
            }

            var node = JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            var entries = node?["entries"] as JsonArray ?? [];
            RankingCache[path] = (DateTime.UtcNow, entries);
            return entries;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            this._logger.LogWarning(ex, "Ranking {Path} failed.", path);
            return cached.Entries;
        }
    }

    /// <summary>Gets guide information of the server API (<c>api/guide/...</c>) as JSON text; cached for a while.</summary>
    /// <param name="path">The path below <c>api/guide/</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The JSON text; <c>null</c> if the server couldn't deliver it and nothing is cached.</returns>
    public async Task<string?> GetGuideAsync(string path, CancellationToken cancellationToken)
    {
        if (GuideCache.TryGetValue(path, out var cached) && DateTime.UtcNow - cached.Time < GuideCacheDuration)
        {
            return cached.Json;
        }

        try
        {
            using var request = this.CreateRequest(HttpMethod.Get, "api/guide/" + path);
            using var response = await this._client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                this._logger.LogWarning("Guide {Path} failed with {Status}.", path, (int)response.StatusCode);
                return cached.Json;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            GuideCache[path] = (DateTime.UtcNow, json);
            return json;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            this._logger.LogWarning(ex, "Guide {Path} failed.", path);
            return cached.Json;
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var options = this._options.CurrentValue;
        var request = new HttpRequestMessage(method, new Uri(new Uri(options.ApiUrl.TrimEnd('/') + "/"), path));
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            request.Headers.Add("X-Api-Key", options.ApiKey.Trim());
        }

        return request;
    }
}
