// <copyright file="FarmSpotMapMarkersConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

/// <summary>
/// The configuration of the <see cref="FarmSpotMapMarkersPlugIn"/>.
/// </summary>
public class FarmSpotMapMarkersConfiguration
{
    /// <summary>
    /// Gets or sets the number of monsters of a monster spawn area which is a farm spot.
    /// </summary>
    [Display(Name = "Monsters per spot", Description = "The number of monsters of a monster spawn area which is shown as a farm spot.")]
    public short MonstersPerSpot { get; set; } = 8;

    /// <summary>
    /// Gets or sets the maximum width and height of a monster spawn area which is a farm spot, in tiles.
    /// </summary>
    [Display(Name = "Maximum spot size", Description = "The maximum width and height in tiles of a monster spawn area which is shown as a farm spot.")]
    public byte MaximumSpotSize { get; set; } = 3;
}
