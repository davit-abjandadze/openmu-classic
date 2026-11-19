// <copyright file="HuntingJewelConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.HuntingRewards;

/// <summary>
/// The configuration of the <see cref="HuntingJewelPlugIn"/>.
/// </summary>
public class HuntingJewelConfiguration
{
    /// <summary>
    /// Gets or sets the hunting time for one jewel.
    /// </summary>
    [Display(Name = "Hunting time per jewel")]
    public TimeSpan HuntingTimePerJewel { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Gets or sets the maximum number of jewels per day.
    /// </summary>
    [Display(Name = "Maximum jewels per day")]
    public int MaximumJewelsPerDay { get; set; } = 8;

    /// <summary>
    /// Gets or sets the maximum time between two kills which counts as hunting time.
    /// A longer pause counts only with this time, so idle time doesn't count.
    /// </summary>
    [Display(Name = "Maximum time between kills", Description = "The time between two monster kills counts as hunting time, but at most this time. So idle time doesn't count.")]
    public TimeSpan MaximumTimeBetweenKills { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the jewels, of which one is randomly chosen, as "group/number" of the item definition.
    /// </summary>
    [Display(Name = "Jewels", Description = "The jewels, of which one is chosen randomly, as group/number of the item, e.g. 14/13 for the Jewel of Bless.")]
    public IList<string> Jewels { get; set; } = ["14/13", "14/14", "12/15", "14/16", "14/22"];
}
