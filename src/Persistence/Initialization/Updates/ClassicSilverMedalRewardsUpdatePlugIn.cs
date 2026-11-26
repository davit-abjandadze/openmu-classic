// <copyright file="ClassicSilverMedalRewardsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;
using MUnique.OpenMU.GameLogic.TeamVsTeam;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Changes the rewards of the Devil Square, the Blood Castle and the Team vs Team on an existing classic (Season 1 content)
/// database to silver medals, and removes the configuration of the former "/medals" chat command plugin,
/// see <see cref="MiniGameMedalsPlugIn"/> and <see cref="TeamVsTeamPlugIn"/>.
/// </summary>
/// <remarks>
/// The 1st place of the Devil Square and the winner of the Blood Castle got a gold medal, which was exchanged with /medals.
/// They get silver medals now, which pay at the medal merchant. The other medal rewards of the configuration are kept.
/// Version 2 replaces the points of the Team vs Team, which weren't used for anything, by silver medals.
/// The other settings of the Team vs Team are kept.
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicSilverMedalRewardsUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicSilverMedalRewardsUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("79EF46F0-F017-4137-8791-150CA361C2CB")]
public class ClassicSilverMedalRewardsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: silver medals as event rewards";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "The 1st place of the Devil Square gets 4 and the winner of the Blood Castle 3 silver medals instead of a gold medal. The best player of the winning Team vs Team team gets 3 and the other members 1 silver medal instead of points. The /medals command is removed; the silver medals pay at the medal merchant. Only applies to the classic (Season 1 content) data.";

    /// <summary>
    /// The type id of the former "/medals" chat command plugin, which exchanged gold and silver medals for items.
    /// The plugin doesn't exist anymore, so its configuration is removed.
    /// </summary>
    internal static readonly Guid RemovedMedalsChatCommandPlugInId = new("3E9D4B27-6C1A-4D85-B0F3-8A2E5C7D1B11");

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    /// <remarks>Version 2 replaces the points of the Team vs Team by silver medals.</remarks>
    public override int Version => 2;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 25, 18, 00, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 11, 25, 19, 00, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (!ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            return;
        }

        var referenceHandler = new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler();
        if (gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == typeof(MiniGameMedalsPlugIn).GUID) is { } medals)
        {
            var defaults = new MiniGameMedalsConfiguration();
            var configuration = medals.GetConfiguration<MiniGameMedalsConfiguration>(referenceHandler) ?? defaults;
            configuration.DevilSquareWinnerSilverMedals = defaults.DevilSquareWinnerSilverMedals;
            configuration.BloodCastleWinnerSilverMedals = defaults.BloodCastleWinnerSilverMedals;
            medals.SetConfiguration(configuration, referenceHandler);
        }

        if (gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == typeof(TeamVsTeamPlugIn).GUID) is { } teamVsTeam)
        {
            var defaults = TeamVsTeamConfiguration.Default;
            var configuration = teamVsTeam.GetConfiguration<TeamVsTeamConfiguration>(referenceHandler) ?? defaults;
            configuration.MostValuablePlayerSilverMedals = defaults.MostValuablePlayerSilverMedals;
            configuration.WinnerSilverMedals = defaults.WinnerSilverMedals;
            teamVsTeam.SetConfiguration(configuration, referenceHandler);
        }

        foreach (var removed in gameConfiguration.PlugInConfigurations.Where(c => c.TypeId == RemovedMedalsChatCommandPlugInId).ToList())
        {
            gameConfiguration.PlugInConfigurations.Remove(removed);
            await context.DeleteAsync(removed).ConfigureAwait(false);
        }
    }
}
