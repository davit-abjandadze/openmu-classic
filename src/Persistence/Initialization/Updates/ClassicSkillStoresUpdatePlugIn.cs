// <copyright file="ClassicSkillStoresUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds all skill scrolls and orbs of a class to the merchant store of that class in an existing classic (Season 1 content) database:
/// Dark Wizard at Pasi the Mage, Dark Knight at Hanzo the Blacksmith and Fairy Elf at Elf Lala.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicSkillStoresUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicSkillStoresUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("3E8B5D12-7A40-4C69-9F2B-1D6E8A4C7B35")]
public class ClassicSkillStoresUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: skill scrolls and orbs in the stores";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Pasi the Mage sells all Dark Wizard scrolls, Hanzo the Blacksmith all Dark Knight orbs and Elf Lala all Fairy Elf orbs. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 19, 22, 00, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            await ClassicMerchantStores.ApplyAsync(context, gameConfiguration).ConfigureAwait(false);
        }
    }
}
