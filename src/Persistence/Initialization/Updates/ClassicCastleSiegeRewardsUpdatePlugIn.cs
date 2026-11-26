// <copyright file="ClassicCastleSiegeRewardsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Configures the Castle Siege of an existing classic (Season 1 content) database: the participants receive
/// Boxes of Kundun +4 and monsters drop the Sign of Lord (Rena +3), see <see cref="ClassicCastleSiege"/>.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicCastleSiegeRewardsUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicCastleSiegeRewardsUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("C4BE52B2-4F50-470F-AFC7-904E222C4BD7")]
public class ClassicCastleSiegeRewardsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: Castle Siege rewards and Sign of Lord drop";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Every guild member who fought at least 30 minutes in the Castle Siege receives 5 Boxes of Kundun +4 (offline participants when they enter the game). Monsters of at least level 50 drop the Sign of Lord (Rena +3), which guilds need to register for the siege, with a chance of 0.5 %. Overwrites the participation reward of the Castle Siege configuration. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override IEnumerable<UpdateDependency> DependsOn => [typeof(AddCastleSiegeDataUpdatePlugIn).GUID];

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicCastleSiege.Apply(context, gameConfiguration);
        }

        return ValueTask.CompletedTask;
    }
}
