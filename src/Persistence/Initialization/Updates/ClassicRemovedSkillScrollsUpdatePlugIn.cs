// <copyright file="ClassicRemovedSkillScrollsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Deactivates the scrolls of Decay, Ice Storm and Nova and removes them from the merchant stores,
/// in an existing classic (Season 1 content) database.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicRemovedSkillScrollsUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicRemovedSkillScrollsUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("A6C1F4E2-58B3-4D07-9E2A-7B4D13C9F861")]
public class ClassicRemovedSkillScrollsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: remove Decay, Ice Storm and Nova";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "The scrolls of Decay, Ice Storm and Nova no longer drop and are no longer sold by Pasi the Mage. Characters which already learned the skills keep them. Only applies to the classic (Season 1 content) data.";

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
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicContentRestriction.DeactivateRemovedSkillItems(gameConfiguration);
            await ClassicMerchantStores.ApplyAsync(context, gameConfiguration).ConfigureAwait(false);
        }
    }
}
