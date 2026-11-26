// <copyright file="ClassicCastleSiegePlugInsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Activates all plugins of the Castle Siege (see <see cref="ClassicPlugInDefaults.GetCastleSiegePlugInTypes"/>)
/// and the suppliers of the Valley of Loren (see <see cref="ClassicCastleSiege.SupplierNumbers"/>) on an existing classic
/// (Season 1 content) database.
/// </summary>
/// <remarks>
/// An existing database may have some of these plugins inactive, e.g. the packet handler group of the Castle Siege (C1 B2),
/// so that the guardsman opens no registration and the client gets no status, marks, taxes or owner logo.
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicCastleSiegePlugInsUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicCastleSiegePlugInsUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("983FF6E8-E02F-48C1-AC07-BACDC7922348")]
public class ClassicCastleSiegePlugInsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: Castle Siege plugins and suppliers";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Activates all plugins of the Castle Siege: the guild registration at the guardsman, the marks, the status, the taxes, the owner logo, the defense, the siege weapons, the Life Stone and the siege potions. Also activates Pamela and Angela the Suppliers in the Valley of Loren, who sell potions, arrows and bolts to the attackers and defenders. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 26, 20, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override IEnumerable<UpdateDependency> DependsOn => [typeof(AddCastleSiegeDataUpdatePlugIn).GUID];

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicPlugInDefaults.ActivateCastleSiegePlugIns(gameConfiguration);
            ClassicCastleSiege.ActivateSuppliers(gameConfiguration);
        }

        return ValueTask.CompletedTask;
    }
}
