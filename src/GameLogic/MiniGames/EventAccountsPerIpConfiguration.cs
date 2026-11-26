// <copyright file="EventAccountsPerIpConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// The configuration of the <see cref="EventAccountsPerIpPlugIn"/>.
/// </summary>
public class EventAccountsPerIpConfiguration
{
    /// <summary>
    /// Gets or sets the maximum number of accounts of one IP address which take part in the same event.
    /// Values below 1 count as 1.
    /// </summary>
    [Display(Name = nameof(Properties.PlugInResources.EventAccountsPerIpConfiguration_MaximumAccountsPerIp_Caption), ResourceType = typeof(Properties.PlugInResources))]
    public int MaximumAccountsPerIp { get; set; } = 1;
}
