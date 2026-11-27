// <copyright file="ClassicFarmSpotsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the farm spots of the classic (Season 1 content) data to an existing database, see <see cref="ClassicFarmSpots"/>.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// Version 2 replaces the three big spots of 12 monsters per map by 35 tiny spots of 8 monsters.
/// Version 3 adds the hand-picked spots of Devias (Elite Yeti, Yeti and Worm).
/// Version 4 adds the hand-picked Arena spots.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicFarmSpotsUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicFarmSpotsUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("3F7A2C91-6E4B-4D58-A0C3-9B1E5F2D8A64")]
public class ClassicFarmSpotsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: farm spots";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Adds 35 farm spots to each of the original farming maps, plus hand-picked spots in Devias and Arena. Each spot spawns 8 monsters of the same kind in a 3x3 square. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override int Version => 4;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 20, 22, 00, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 11, 27, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration, int? installedVersion)
    {
        if (installedVersion is null or < 3)
        {
            return this.ApplyAsync(context, gameConfiguration);
        }

        // Existing farm spots can be customized in the map editor.
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicFarmSpots.ApplyArena(context, gameConfiguration);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicFarmSpots.Apply(context, gameConfiguration);
        }

        return ValueTask.CompletedTask;
    }
}
