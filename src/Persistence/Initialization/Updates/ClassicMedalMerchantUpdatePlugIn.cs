// <copyright file="ClassicMedalMerchantUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the medal merchant to an existing classic (Season 1 content) database: Thompson the Merchant stands next to
/// the fountain of Lorencia and sells the Boxes of Kundun for silver medals, see <see cref="ClassicMedalMerchant"/>.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// Version 2 sells only the Box of Kundun +4 and +5, for 10 and 15 silver medals, instead of the +1 to +5 for 4 to 64 silver medals.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicMedalMerchantUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicMedalMerchantUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("AD5DDF36-C9FD-407B-A349-5E3618763482")]
public class ClassicMedalMerchantUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: medal merchant";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Thompson the Merchant stands next to the fountain of Lorencia and sells the Box of Kundun +4 for 10 and the Box of Kundun +5 for 15 silver medals. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override int Version => 2;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 20, 20, 00, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            await ClassicMedalMerchant.ApplyAsync(context, gameConfiguration).ConfigureAwait(false);
            ClassicPlugInDefaults.ActivateMedalMerchant(gameConfiguration, context, new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        }
    }
}
