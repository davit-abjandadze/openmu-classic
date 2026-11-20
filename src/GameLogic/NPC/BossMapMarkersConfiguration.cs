// <copyright file="BossMapMarkersConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

/// <summary>
/// The configuration of the <see cref="BossMapMarkersPlugIn"/>.
/// </summary>
public class BossMapMarkersConfiguration
{
    /// <summary>
    /// Gets or sets the numbers of the monster definitions which are shown as bosses on the map.
    /// </summary>
    [Display(Name = "Boss monster numbers", Description = "The numbers of the monsters which are shown as bosses on the map.")]
    public IList<short> BossMonsterNumbers { get; set; } =
    [
        38, // Balrog
        49, // Hydra
        59, // Zaikan
        63, // Death Beam Knight
        77, // Dark Phoenix
        309, // Hell Maine
    ];
}
