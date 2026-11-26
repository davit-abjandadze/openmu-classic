// <copyright file="ClassicJewelryDropsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Makes the rings and pendants of an existing classic (Season 1 content) database rarer, see <see cref="ClassicJewelryDrops"/>.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicJewelryDropsUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicJewelryDropsUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("B3E7A915-4C2D-4F86-9D1B-6A08C5F2E473")]
public class ClassicJewelryDropsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: rarer rings and pendants";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Rings and pendants are no longer part of the random and excellent item drops of the monsters. Instead, monsters drop a normal ring or pendant with a chance of 0.5% and, from monster level 50, an excellent one with a chance of 0.02%, on all maps with the common jewel drop. The Box of Kundun +2 and +3 stay the main source of the excellent ones. Only applies to the classic (Season 1 content) data.";

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
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicJewelryDrops.Apply(context, gameConfiguration);
        }

        return ValueTask.CompletedTask;
    }
}
