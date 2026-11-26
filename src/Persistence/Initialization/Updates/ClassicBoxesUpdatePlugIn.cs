// <copyright file="ClassicBoxesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Changes the boxes of an existing classic (Season 1 content) database: monsters drop the Box of Heaven,
/// which drops a normal +7 to +11 weapon or armor piece, Caren the Barmaid sells the Box of Kundun +1 to +5,
/// and the excellent Archangel weapons drop from the Box of Kundun +5 instead of the +4.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicBoxesUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicBoxesUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("65663747-3C6C-44B5-B08A-E61A0AFE491E")]
public class ClassicBoxesUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: Box of Heaven and Boxes of Kundun";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Monsters of level 30 and above drop the Box of Heaven (0.1% chance), which always drops a normal (not excellent) +7 to +11 weapon or armor piece, Caren the Barmaid sells the Box of Kundun +1 to +5, and the excellent Archangel weapons drop from the Box of Kundun +5 instead of the +4. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 20, 12, 00, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    /// <remarks>
    /// Version 2 moves the Archangel weapons from the Box of Kundun +4 to the +5.
    /// </remarks>
    public override int Version => 2;

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 11, 20, 18, 00, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    /// <remarks>
    /// The Box of Heaven drops only active items, so the Magic Gladiator items have to be activated first.
    /// Both updates have the same <see cref="UpdatedAt"/>, so without the dependency their order would depend on their keys.
    /// </remarks>
    public override IEnumerable<UpdateDependency> DependsOn => [typeof(ClassicMagicGladiatorUpdatePlugIn).GUID];

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            await ClassicBoxes.ApplyAsync(context, gameConfiguration).ConfigureAwait(false);
        }
    }
}
