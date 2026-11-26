using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using OldMu.Site;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection("Site"));
builder.Services.AddSingleton<ChallengeService>();
builder.Services.AddSingleton<RegistrationLimiter>();
builder.Services.AddHttpClient<GameServerApi>(client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddRateLimiter(limiter =>
{
    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiter.OnRejected = (context, cancellationToken) =>
        new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(new { error = "Too many requests. Wait a few minutes and try again." }, cancellationToken));

    // Against hammering the form itself; the number of created accounts is limited separately.
    limiter.AddPolicy("register", context => RateLimitPartition.GetFixedWindowLimiter(
        RegistrationLimiter.GetKey(context.Connection.RemoteIpAddress),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(10) }));
    limiter.AddPolicy("challenge", context => RateLimitPartition.GetFixedWindowLimiter(
        RegistrationLimiter.GetKey(context.Connection.RemoteIpAddress),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(10) }));
});

var app = builder.Build();
var siteOptions = app.Services.GetRequiredService<IOptions<SiteOptions>>().Value;
if (siteOptions.TrustForwardedHeaders)
{
    var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };

    // The proxy runs in the same docker network, whose address isn't known in advance.
    forwarded.KnownIPNetworks.Clear();
    forwarded.KnownProxies.Clear();
    app.UseForwardedHeaders(forwarded);
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "same-origin";
    headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; style-src 'self' https://fonts.googleapis.com; font-src https://fonts.gstatic.com; worker-src 'self'; frame-ancestors 'none'";
    await next(context).ConfigureAwait(false);
});

app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", async (GameServerApi api, IOptionsMonitor<SiteOptions> options, CancellationToken cancellationToken) =>
{
    var status = await api.GetStatusAsync(cancellationToken).ConfigureAwait(false);
    return Results.Ok(new { online = status.Online, players = status.Players, downloadUrl = options.CurrentValue.DownloadUrl, discordUrl = options.CurrentValue.DiscordUrl });
});

// The boards of the ranking page and the server API path of each.
var rankingBoards = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["characters"] = "characters?limit=100",
    ["pvp-week"] = "pvp?allTime=false&limit=50",
    ["pvp-all"] = "pvp?allTime=true&limit=50",
    ["devil-square"] = "minigames/DevilSquare",
    ["blood-castle"] = "minigames/BloodCastle",
};

app.MapGet("/api/rankings/{board}", async (string board, GameServerApi api, CancellationToken cancellationToken) =>
{
    if (!rankingBoards.TryGetValue(board, out var path))
    {
        return Results.NotFound(new { error = "Unknown ranking." });
    }

    var status = await api.GetStatusAsync(cancellationToken).ConfigureAwait(false);
    if (await api.GetRankingAsync(path, cancellationToken).ConfigureAwait(false) is not { } entries)
    {
        return Results.Json(new { error = "The ranking is not available right now." }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    // The server's fields are copied into new objects: the cached nodes must not get another parent,
    // and only the fields which the page shows are passed on.
    var rows = entries.OfType<JsonObject>().Select(entry =>
    {
        var name = (string?)(entry["name"] ?? entry["characterName"]) ?? string.Empty;
        return new
        {
            rank = (int?)entry["rank"] ?? 0,
            name,
            characterClass = (string?)(entry["characterClass"] ?? entry["className"]) ?? string.Empty,
            level = (int?)entry["level"],
            resets = (int?)entry["resets"],
            prestige = (int?)entry["prestige"],
            kills = (int?)entry["kills"],
            deaths = (int?)entry["deaths"],
            assists = (int?)entry["assists"],
            score = (long?)entry["totalScore"],
            games = (int?)entry["gameCount"],
            online = status.Names.Contains(name),
        };
    }).ToList();
    return Results.Ok(new { board, players = status.Players, serverOnline = status.Online, entries = rows });
});

app.MapGet("/api/guide/exchange", async (GameServerApi api, CancellationToken cancellationToken) =>
    await api.GetGuideAsync("exchange", cancellationToken).ConfigureAwait(false) is { } json
        ? Results.Text(json, "application/json")
        : Results.Json(new { error = "The exchange list is not available right now." }, statusCode: StatusCodes.Status503ServiceUnavailable));

app.MapGet("/api/register/challenge", (ChallengeService challenges) =>
{
    var (token, bits) = challenges.Issue();
    return Results.Ok(new { token, bits });
}).RequireRateLimiting("challenge");

app.MapPost("/api/register", async (RegisterRequest request, HttpContext context, ChallengeService challenges, RegistrationLimiter limiter, GameServerApi api, ILogger<Program> logger) =>
{
    var ip = RegistrationLimiter.GetKey(context.Connection.RemoteIpAddress);

    // The "website" field is hidden from people; only form-filling bots put something in.
    if (!string.IsNullOrEmpty(request.Website))
    {
        logger.LogInformation("Registration from {Ip} dropped by the honeypot.", ip);
        return Results.BadRequest(new { error = "The security check failed. Try again." });
    }

    var login = request.Login?.Trim() ?? string.Empty;
    var password = request.Password ?? string.Empty;
    if (!Validation.LoginName().IsMatch(login))
    {
        return Results.BadRequest(new { error = "The login must have 4 to 10 letters (a-z) or digits.", field = "login" });
    }

    if (!Validation.Password().IsMatch(password))
    {
        return Results.BadRequest(new { error = "The password must have 6 to 20 characters, without spaces.", field = "password" });
    }

    if (password != request.PasswordRepeat)
    {
        return Results.BadRequest(new { error = "The passwords don't match.", field = "repeat" });
    }

    if (string.Equals(login, password, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "The password must not be the same as the login.", field = "password" });
    }

    if (challenges.Verify(request.Token, request.Nonce) is { } challengeError)
    {
        return Results.BadRequest(new { error = challengeError });
    }

    if (limiter.TryReserve(ip, out var reservation) is { } limitError)
    {
        return Results.Json(new { error = limitError }, statusCode: StatusCodes.Status429TooManyRequests);
    }

    var result = await api.CreateAccountAsync(login, password, context.RequestAborted).ConfigureAwait(false);
    if (result != CreateAccountResult.Created)
    {
        limiter.Release(reservation);
    }

    switch (result)
    {
        case CreateAccountResult.Created:
            logger.LogInformation("Account {Login} created from {Ip}.", login, ip);
            return Results.Ok(new { login });
        case CreateAccountResult.Taken:
            return Results.Conflict(new { error = "This login is already taken.", field = "login" });
        case CreateAccountResult.Invalid:
            return Results.BadRequest(new { error = "The server refused this login or password.", field = "login" });
        default:
            return Results.Json(new { error = "Registration is not available right now. Please try again later." }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).RequireRateLimiting("register");

app.Run();

/// <summary>The registration form.</summary>
/// <param name="Login">The login name.</param>
/// <param name="Password">The password.</param>
/// <param name="PasswordRepeat">The repeated password.</param>
/// <param name="Website">The honeypot field, which must stay empty.</param>
/// <param name="Token">The proof-of-work challenge.</param>
/// <param name="Nonce">The solution of the challenge.</param>
internal sealed record RegisterRequest(string? Login, string? Password, string? PasswordRepeat, string? Website, string? Token, string? Nonce);

/// <summary>The rules for login names and passwords.</summary>
/// <remarks>
/// The client takes at most 10 characters for the login and 20 for the password. Logins are kept
/// to letters and digits, which are easy to type and can't be confused.
/// </remarks>
internal static partial class Validation
{
    [GeneratedRegex("^[A-Za-z0-9]{4,10}$")]
    public static partial Regex LoginName();

    [GeneratedRegex("^[\\x21-\\x7E]{6,20}$")]
    public static partial Regex Password();
}
