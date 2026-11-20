// <copyright file="FarmSpotMapMarkersPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.World;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Shows the farm spots of the current map as dots on the map of the client (Tab). A farm spot is a tiny
/// monster spawn area in which a few monsters of the same kind stand together, e.g. the 8 monsters on a
/// 3 x 3 square of the classic server. The spots are sent when the player enters a map.
/// </summary>
[Guid("8D4B6E21-3A7C-4F95-B0E8-61C2D9A47F13")]
[PlugIn]
[Display(Name = "Farm spot markers on the map", Description = "Shows the farm spots of the current map as dots on the map of the open source client.")]
public class FarmSpotMapMarkersPlugIn : IObjectAddedToMapPlugIn, ISupportCustomConfiguration<FarmSpotMapMarkersConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public FarmSpotMapMarkersConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new FarmSpotMapMarkersConfiguration();

    /// <inheritdoc />
    public async ValueTask ObjectAddedToMapAsync(GameMap map, ILocateable addedObject)
    {
        if (addedObject is not Player player)
        {
            return;
        }

        var spots = this.GetSpots(map.Definition);
        await player.InvokeViewPlugInAsync<IShowFarmSpotMapMarkersPlugIn>(p => p.ShowFarmSpotMapMarkersAsync(map.Definition.Number, spots)).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the centres of the farm spots of a map.
    /// </summary>
    /// <param name="map">The map definition.</param>
    /// <returns>The centres of the farm spots, without duplicates.</returns>
    internal IReadOnlyCollection<Point> GetSpots(GameMapDefinition map)
    {
        var configuration = this.Configuration ??= new FarmSpotMapMarkersConfiguration();
        return map.MonsterSpawns
            .Where(s => s.SpawnTrigger == SpawnTrigger.Automatic
                        && s.Quantity == configuration.MonstersPerSpot
                        && s.MonsterDefinition?.ObjectKind == NpcObjectKind.Monster
                        && Math.Abs(s.X2 - s.X1) < configuration.MaximumSpotSize
                        && Math.Abs(s.Y2 - s.Y1) < configuration.MaximumSpotSize)
            .Select(s => new Point((byte)((s.X1 + s.X2) / 2), (byte)((s.Y1 + s.Y2) / 2)))
            .Distinct()
            .ToList();
    }
}
