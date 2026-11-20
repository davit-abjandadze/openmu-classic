// <copyright file="ClassicMagicGladiatorUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Activates the Magic Gladiator and its items on an existing classic (Season 1 content) database,
/// and activates the <see cref="GameLogic.PlugIns.UnlockCharacterClass.UnlockedCharacterClassesPlugIn"/>,
/// so every account can create it from the start.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicMagicGladiatorUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicMagicGladiatorUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("6F1D8B32-4C7E-4A95-8E2B-3D9A5C1F7E08")]
public class ClassicMagicGladiatorUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: Magic Gladiator";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Activates the Magic Gladiator and its items, and lets every account create it from the start. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 20, 18, 00, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicContentRestriction.ActivateAllowedClasses(gameConfiguration);
            ClassicPlugInDefaults.ActivateUnlockedCharacterClasses(gameConfiguration, context, new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        }

        return ValueTask.CompletedTask;
    }
}
