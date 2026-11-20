// <copyright file="ClassicBossStrengthUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Makes the field bosses of an existing classic (Season 1 content) database much stronger, so that they
/// don't die as fast as normal monsters, see <see cref="ClassicBosses.BossStrength"/>.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicBossStrengthUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicBossStrengthUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("6E1F3B2A-9C47-4D58-B0A2-7F3E51C8D946")]
public class ClassicBossStrengthUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: Boss strength";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Hydra and Balrog get 800k health, Zaikan and Death Beam Knight 2M, Dark Phoenix 5M, Hell Maine 7M (40% stronger than the Dark Phoenix), together with much higher damage, defense, attack rate and defense rate, so that they need minutes to kill instead of seconds. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 20, 21, 00, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicBosses.ApplyStrength(context, gameConfiguration);
        }

        return ValueTask.CompletedTask;
    }
}
