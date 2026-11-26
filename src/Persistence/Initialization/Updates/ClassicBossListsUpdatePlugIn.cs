// <copyright file="ClassicBossListsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Resets the boss lists on an existing classic (Season 1 content) database, so that only the field bosses
/// are shown as bosses on the map, with their respawn time, see <see cref="ClassicPlugInDefaults.ResetBossLists"/>.
/// It also removes the configuration of the former "/bosses" chat command plugin, which was replaced by the
/// <c>BossesChatCommandPlugIn</c>.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicBossListsUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicBossListsUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("D4E81B37-2C6A-4F95-B0E3-7A19C5F2846D")]
public class ClassicBossListsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: field bosses on the map";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Only the field bosses (Balrog, Hydra, Zaikan, Death Beam Knight, Dark Phoenix and Hell Maine) are shown as bosses on the map and listed by /bosses, with their respawn time. Only applies to the classic (Season 1 content) data.";

    /// <summary>
    /// The type id of the former "/bosses" chat command plugin, which only listed the bosses which are alive.
    /// The plugin doesn't exist anymore, so its configuration is removed.
    /// </summary>
    internal static readonly Guid RemovedBossStatusChatCommandPlugInId = new("7C4B1E92-5A3D-4F8B-9E26-1D0C3A7B5F01");

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 20, 22, 00, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    /// <remarks>
    /// Version 2 adds Hell Maine, which spawns in the Arena.
    /// Version 3 removes the configuration of the former "/bosses" chat command plugin.
    /// </remarks>
    public override int Version => 3;

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 11, 20, 23, 30, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (!ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            return;
        }

        ClassicPlugInDefaults.ResetBossLists(gameConfiguration, new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        foreach (var removed in gameConfiguration.PlugInConfigurations.Where(c => c.TypeId == RemovedBossStatusChatCommandPlugInId).ToList())
        {
            gameConfiguration.PlugInConfigurations.Remove(removed);
            await context.DeleteAsync(removed).ConfigureAwait(false);
        }
    }
}
