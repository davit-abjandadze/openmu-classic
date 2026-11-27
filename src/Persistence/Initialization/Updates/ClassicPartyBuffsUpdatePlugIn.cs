// <copyright file="ClassicPartyBuffsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Lets Dark Wizard, Dark Knight and Fairy Elf buff other players again on an existing classic database,
/// which an earlier version of the classic data restricted to the caster, see <see cref="ClassicContentRestriction.RestoreBuffTargets"/>.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicPartyBuffsUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicPartyBuffsUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("5C3E9A71-2B84-4D6F-A0E5-97D1B4C82F36")]
public class ClassicPartyBuffsUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string Name => MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicPartyBuffsUpdatePlugIn_Name;

    /// <inheritdoc />
    public override string Description => MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicPartyBuffsUpdatePlugIn_Description;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicContentRestriction.RestoreBuffTargets(gameConfiguration);
        }

        return ValueTask.CompletedTask;
    }
}
