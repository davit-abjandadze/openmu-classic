// <copyright file="GuideController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.Persistence.AdminAuth;
using MUnique.OpenMU.Web.AdminPanel.Auth;
using MUnique.OpenMU.Web.AdminPanel.Operations;

/// <summary>
/// Reads game information for the guide of a website, from the current configuration.
/// </summary>
[ApiController]
[Route("api/guide")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Roles = nameof(AdminRole.Viewer))]
public sealed class GuideController : ControllerBase
{
    /// <summary>Reads what the <c>/exchange</c> chat command offers.</summary>
    /// <param name="operations">The exchange operations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The settings, the items which can be obtained and the items which can be given in.</returns>
    [HttpGet("exchange")]
    public async ValueTask<IActionResult> ExchangeAsync([FromServices] ExcellentExchangeReadOperations operations, CancellationToken cancellationToken)
        => this.Ok(await operations.GetAsync(cancellationToken).ConfigureAwait(false));
}
