// <copyright file="ClassicRelativeResetExperienceRatesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Replaces the absolute default rates of the <see cref="GameLogic.Resets.ResetExperienceRatePlugIn"/> (540 at zero resets)
/// on an existing classic (Season 1 content) database by multipliers relative to the configured experience rate.
/// The plugin multiplies the configured rate, so an experience rate of 10 resulted in an effective rate of 5400.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicRelativeResetExperienceRatesUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicRelativeResetExperienceRatesUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("5E0B7C3A-92D1-4F68-A7E4-3C1D8B6F2A95")]
public class ClassicRelativeResetExperienceRatesUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: relative experience rates by resets";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Replaces the default experience rates by resets (540 at zero resets), which were multiplied with the configured experience rate, by multipliers relative to it (1.0 at zero resets). A changed configuration is kept. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 20, 18, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicPlugInDefaults.ReplaceAbsoluteResetExperienceRates(gameConfiguration, new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        }

        return ValueTask.CompletedTask;
    }
}
