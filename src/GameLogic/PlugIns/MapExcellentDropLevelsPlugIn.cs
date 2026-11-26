// <copyright file="MapExcellentDropLevelsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sets the drop levels of the excellent items (weapons, shields, armor, rings and pendants) which monsters drop
/// on a map, independent of the level of the monster. For example, the monsters of Tarkan can drop
/// excellent items from the Legendary, Dragon and Guardian sets, but nothing below, and the monsters of Icarus
/// nothing of the Grand Soul or Dark Phoenix sets.
/// </summary>
/// <remarks>
/// It applies to the randomly selected excellent items which the <see cref="DefaultDropGenerator"/> picks for a monster drop.
/// Without it, a monster drops excellent items by its level minus the <see cref="GameConfiguration.ExcellentItemDropLevelDelta"/>.
/// Normal items still drop by the monster level, and items which a drop item group lists explicitly, e.g. of a Box of Kundun, aren't affected.
/// </remarks>
[PlugIn]
[Display(Name = nameof(PlugInResources.MapExcellentDropLevelsPlugIn_Name), Description = nameof(PlugInResources.MapExcellentDropLevelsPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("E3A7C5D1-6B42-4F98-8D1E-2C9F7A0B4E63")]
public class MapExcellentDropLevelsPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<MapExcellentDropLevelsConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public MapExcellentDropLevelsConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the drop levels of the excellent items on the map, if the plugin is active and the map is configured.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="map">The map.</param>
    /// <returns>The drop levels of the excellent items on the map, or <c>null</c> when they drop by the monster level.</returns>
    public static MapExcellentDropLevelsConfiguration.MapDropLevels? GetDropLevels(IGameContext gameContext, GameMapDefinition map)
        => gameContext.FeaturePlugIns.GetPlugIn<MapExcellentDropLevelsPlugIn>()?.Configuration?.Maps.FirstOrDefault(m => m.MapNumber == map.Number);

    /// <inheritdoc />
    public object CreateDefaultConfig() => new MapExcellentDropLevelsConfiguration();
}
