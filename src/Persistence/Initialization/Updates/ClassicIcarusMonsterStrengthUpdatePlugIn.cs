// <copyright file="ClassicIcarusMonsterStrengthUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Makes Phantom Knight, Drakan, Alpha Crust and Great Drakan of an existing classic (Season 1 content) database
/// four times as strong, see <see cref="ClassicIcarusMonsters"/>.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicIcarusMonsterStrengthUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicIcarusMonsterStrengthUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("3B9D7E15-A4C2-4F86-9E1B-52D0C7A86F31")]
public class ClassicIcarusMonsterStrengthUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: Icarus monster strength";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Phantom Knight, Drakan, Alpha Crust and Great Drakan get four times their health, damage, defense, attack rate and defense rate (e.g. Great Drakan 200k health). Customized values of these stats are overwritten. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 25, 14, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicIcarusMonsters.Apply(context, gameConfiguration);
        }

        return ValueTask.CompletedTask;
    }
}
