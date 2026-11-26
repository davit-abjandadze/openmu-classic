// <copyright file="RankingController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.AdminAuth;
using MUnique.OpenMU.Web.AdminPanel.Auth;
using MUnique.OpenMU.Web.AdminPanel.Operations;

/// <summary>
/// Reads the rankings, e.g. for the ranking page of a website.
/// </summary>
/// <remarks>
/// The queries aren't cached here, so a public website should cache the results itself.
/// </remarks>
[ApiController]
[Route("api/rankings")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Roles = nameof(AdminRole.Viewer))]
public sealed class RankingController : ControllerBase
{
    /// <summary>Reads the best characters by resets and level.</summary>
    /// <param name="operations">The ranking operations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="limit">The maximum number of characters (1 to 100).</param>
    /// <returns>The ranking.</returns>
    [HttpGet("characters")]
    public async ValueTask<IActionResult> CharactersAsync([FromServices] RankingReadOperations operations, CancellationToken cancellationToken, [FromQuery] int limit = 50)
        => await this.ExecuteAsync(() => operations.CharactersAsync(limit, cancellationToken)).ConfigureAwait(false);

    /// <summary>Reads the PvP prestige ranking.</summary>
    /// <param name="operations">The ranking operations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="allTime"><c>true</c> for all time; <c>false</c> for the current week.</param>
    /// <param name="limit">The maximum number of characters (1 to 100).</param>
    /// <returns>The ranking.</returns>
    [HttpGet("pvp")]
    public async ValueTask<IActionResult> PvpAsync([FromServices] RankingReadOperations operations, CancellationToken cancellationToken, [FromQuery] bool allTime = false, [FromQuery] int limit = 50)
        => await this.ExecuteAsync(() => operations.PvpAsync(allTime, limit, cancellationToken)).ConfigureAwait(false);

    /// <summary>Reads the leaderboard of a mini game type, like Devil Square or Blood Castle.</summary>
    /// <param name="operations">The mini game operations.</param>
    /// <param name="type">The mini game type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="days">The number of past days which are counted; empty for all time.</param>
    /// <returns>The leaderboard.</returns>
    [HttpGet("minigames/{type}")]
    public async ValueTask<IActionResult> MiniGameAsync([FromServices] MiniGameReadOperations operations, MiniGameType type, CancellationToken cancellationToken, [FromQuery] int? days = null)
        => await this.ExecuteAsync(() => operations.LeaderboardAsync(type, days, cancellationToken)).ConfigureAwait(false);

    private async ValueTask<IActionResult> ExecuteAsync<T>(Func<ValueTask<T>> operation)
    {
        try
        {
            return this.Ok(await operation().ConfigureAwait(false));
        }
        catch (ArgumentException error)
        {
            return this.BadRequest(new { error = error.Message });
        }
    }
}
