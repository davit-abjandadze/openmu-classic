// <copyright file="ClassicBossesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Changes the field bosses of an existing classic (Season 1 content) database: they always drop a Box of Kundun
/// and silver medals, and respawn after a fixed delay, see <see cref="ClassicBosses"/>.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicBossesUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicBossesUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("B0550E1A-7D2C-4E6F-9A31-5C8E2F4D7B21")]
public class ClassicBossesUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: Boss drops and respawn";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Hydra (Atlans) and Balrog (Lost Tower) drop a Box of Kundun +3 and respawn after 70 minutes, Zaikan and Death Beam Knight (Tarkan) a Box of Kundun +4 and respawn after 120 minutes, Dark Phoenix (Icarus) and Hell Maine (spawned in the Arena) a Box of Kundun +5 and respawn after 240 minutes. The bosses also drop a silver medal, the Dark Phoenix and Hell Maine three. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 20, 18, 00, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    /// <remarks>Version 2 adds Hell Maine, which spawns in the Arena.</remarks>
    public override int Version => 2;

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 11, 20, 23, 00, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicBosses.Apply(context, gameConfiguration);
        }

        return ValueTask.CompletedTask;
    }
}
