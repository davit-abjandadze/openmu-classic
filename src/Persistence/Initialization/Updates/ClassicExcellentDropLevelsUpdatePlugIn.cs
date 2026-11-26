// <copyright file="ClassicExcellentDropLevelsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Activates the <see cref="GameLogic.PlugIns.MapExcellentDropLevelsPlugIn"/> on an existing classic (Season 1 content) database,
/// so that the monsters of Tarkan and Icarus drop excellent items between the Legendary, Dragon and Guardian sets and the
/// Grand Soul and Dark Phoenix sets, see <see cref="ClassicPlugInDefaults.CreateExcellentDropLevelsConfiguration"/>.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicExcellentDropLevelsUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicExcellentDropLevelsUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("5F8C2A71-D3E6-4B19-A0C4-7E2B9D16F358")]
public class ClassicExcellentDropLevelsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: excellent item drop levels of Tarkan and Icarus";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "The monsters of Tarkan and Icarus drop excellent weapons, armor and jewelry with a drop level between 50 (Legendary, Dragon and Guardian sets) and 69, so nothing lower and nothing of the Grand Soul, Dark Phoenix, Black Dragon, Divine and Storm Crow level. Normal items still drop by the monster level. Overwrites the configuration of the excellent item drop levels plugin. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 25, 20, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicPlugInDefaults.ActivateExcellentDropLevels(gameConfiguration, context, new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        }

        return ValueTask.CompletedTask;
    }
}
