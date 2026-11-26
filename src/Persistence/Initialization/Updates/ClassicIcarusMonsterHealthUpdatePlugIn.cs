// <copyright file="ClassicIcarusMonsterHealthUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Halves the health and defense of Phantom Knight, Drakan, Alpha Crust and Great Drakan of an existing classic
/// (Season 1 content) database which got four times as strong by the <see cref="ClassicIcarusMonsterStrengthUpdatePlugIn"/>,
/// see <see cref="ClassicIcarusMonsters"/>.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicIcarusMonsterHealthUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicIcarusMonsterHealthUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("8E2F4C61-7B3D-4A95-B0E8-1D6C5A9F2E47")]
public class ClassicIcarusMonsterHealthUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: halved Icarus monster health and defense";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Halves the health and defense of Phantom Knight, Drakan, Alpha Crust and Great Drakan, which were four times as strong as in the original data, to twice the original values (e.g. Great Drakan 100k health). Damage, attack rate and defense rate stay four times as high. Customized values of these stats are overwritten. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 26, 14, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override IEnumerable<UpdateDependency> DependsOn => [typeof(ClassicIcarusMonsterStrengthUpdatePlugIn).GUID];

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
