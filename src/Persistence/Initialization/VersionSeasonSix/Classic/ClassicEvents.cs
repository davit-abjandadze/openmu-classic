// <copyright file="ClassicEvents.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using System.Text.Json.Serialization;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.GameLogic.TeamVsTeam;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The events of the classic server: free Devil Square and Blood Castle three times a day, in which all solo players
/// and parties of a tier share its room, entered by /joinds and /joinbc, once per day and event;
/// the Golden Invasion and the Team vs Team in the night.
/// </summary>
public static class ClassicEvents
{
    /// <summary>
    /// The maximum number of players in the shared room of a tier. When it's full, another room of the tier is opened.
    /// </summary>
    public const int MaximumPlayersPerRoom = 50;

    /// <summary>
    /// The duration in which the instances of a slot can be entered.
    /// </summary>
    public static readonly TimeSpan EntranceDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The duration of the game of an instance.
    /// </summary>
    public static readonly TimeSpan GameDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The duration of a whole slot: entrance, countdown, game and exit, with some reserve.
    /// </summary>
    public static readonly TimeSpan SlotDuration = TimeSpan.FromMinutes(25);

    /// <summary>
    /// The times before a start at which it's announced.
    /// </summary>
    public static readonly IReadOnlyList<TimeSpan> AnnouncementLeadTimes = [TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(2)];

    /// <summary>
    /// The level windows of the Blood Castle tiers 1 to 7, i.e. of the game levels of their definitions.
    /// </summary>
    public static readonly IReadOnlyList<(int Minimum, int Maximum)> BloodCastleTierLevels =
    [
        (10, 60),
        (61, 120),
        (121, 180),
        (181, 240),
        (241, 300),
        (301, 360),
        (361, 400),
    ];

    /// <summary>
    /// The level windows of the Devil Square tiers 1 to 6, i.e. of the game levels of their definitions.
    /// </summary>
    /// <remarks>
    /// The seventh Devil Square came after Season 1: its map and monsters are deactivated by the
    /// <see cref="ClassicContentRestriction"/>. So the sixth tier also covers the levels of the seventh.
    /// </remarks>
    public static readonly IReadOnlyList<(int Minimum, int Maximum)> DevilSquareTierLevels =
    [
        (10, 60),
        (61, 120),
        (121, 180),
        (181, 240),
        (241, 300),
        (301, 400),
    ];

    /// <summary>
    /// The start times of the Devil Square, in the time zone of the server.
    /// </summary>
    public static readonly IReadOnlyList<TimeOnly> DevilSquareTimes = [new(13, 0), new(19, 0), new(22, 0)];

    /// <summary>
    /// The start times of the Blood Castle, in the time zone of the server.
    /// </summary>
    public static readonly IReadOnlyList<TimeOnly> BloodCastleTimes = [new(13, 30), new(19, 30), new(22, 30)];

    /// <summary>
    /// The start time of the Golden Invasion, in the time zone of the server.
    /// </summary>
    public static readonly TimeOnly GoldenInvasionTime = new(23, 0);

    /// <summary>
    /// The start time of the registration of the Team vs Team, in the time zone of the server.
    /// </summary>
    public static readonly TimeOnly TeamVsTeamTime = new(23, 30);

    /// <summary>
    /// Adapts the mini game definitions: free tiers by level (six Devil Squares, seven Blood Castles), entered in instances per party, with double experience.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    public static void ApplyToMiniGames(GameConfiguration gameConfiguration)
    {
        Adapt(gameConfiguration, MiniGameType.DevilSquare, DevilSquareTierLevels);
        Adapt(gameConfiguration, MiniGameType.BloodCastle, BloodCastleTierLevels);
    }

    /// <summary>
    /// Configures the timetables of the event plugins.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void ApplyToPlugIns(GameConfiguration gameConfiguration, ReferenceHandler referenceHandler)
    {
        ApplyToEventStartPlugIns(gameConfiguration, referenceHandler);

        if (Find<MiniGameMedalsPlugIn>(gameConfiguration) is { } medals)
        {
            medals.IsActive = true;
            medals.SetConfiguration(new MiniGameMedalsConfiguration(), referenceHandler);
        }

        if (Find<TeamVsTeamPlugIn>(gameConfiguration) is { } teamVsTeam)
        {
            var configuration = TeamVsTeamConfiguration.Default;
            configuration.Timetable = [TeamVsTeamTime];
            teamVsTeam.IsActive = true;
            teamVsTeam.SetConfiguration(configuration, referenceHandler);
        }

        if (Find<GoldenInvasionPlugIn>(gameConfiguration) is { } golden)
        {
            var configuration = (PeriodicInvasionConfiguration)new GoldenInvasionPlugIn().CreateDefaultConfig();
            configuration.Timetable = [GoldenInvasionTime];
            golden.IsActive = true;
            golden.SetConfiguration(configuration, referenceHandler);
        }
    }

    /// <summary>
    /// Configures the start plugins of the Devil Square and the Blood Castle: three slots a day, announced 5 and 2 minutes before.
    /// It replaces the former configuration, e.g. the single start of the former nightly event.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void ApplyToEventStartPlugIns(GameConfiguration gameConfiguration, ReferenceHandler referenceHandler)
    {
        if (Find<DevilSquareStartPlugIn>(gameConfiguration) is { } devilSquare)
        {
            var configuration = DevilSquareStartConfiguration.Default;
            Configure(configuration, DevilSquareTimes, "Devil Square", GameLogic.MiniGames.JoinMiniGameChatCommandPlugInBase.GetCommand(MiniGameType.DevilSquare));
            devilSquare.IsActive = true;
            devilSquare.SetConfiguration(configuration, referenceHandler);
        }

        if (Find<BloodCastleStartPlugIn>(gameConfiguration) is { } bloodCastle)
        {
            var configuration = BloodCastleStartConfiguration.Default;
            Configure(configuration, BloodCastleTimes, "Blood Castle", GameLogic.MiniGames.JoinMiniGameChatCommandPlugInBase.GetCommand(MiniGameType.BloodCastle));
            bloodCastle.IsActive = true;
            bloodCastle.SetConfiguration(configuration, referenceHandler);
        }
    }

    private static void Configure(InstancedMiniGameStartConfiguration configuration, IEnumerable<TimeOnly> times, string name, string command)
    {
        configuration.Timetable = times.ToList();
        configuration.TaskDuration = SlotDuration;
        configuration.PreStartMessageDelay = TimeSpan.Zero;
        configuration.EntranceDuration = EntranceDuration;
        configuration.GameDuration = GameDuration;
        configuration.AnnouncementLeadTimes = AnnouncementLeadTimes.ToList();
        configuration.UpcomingMessage = $"{name} opens in {{0}} minute(s)! Join with {command} (once per day).";
        configuration.EntranceOpenedMessage = $"{name} is open for {{0}} minute(s)! Join with {command} (once per day).";
        configuration.EntranceClosedMessage = $"{name} entrance closed.";
        configuration.IsSharedPerTier = true;
        configuration.IsLevelSyncEnabled = true;
        configuration.PartySizeScalings = InstancedMiniGameStartConfiguration.CreateDefaultPartySizeScalings();
    }

    private static void Adapt(GameConfiguration gameConfiguration, MiniGameType type, IReadOnlyList<(int Minimum, int Maximum)> tierLevels)
    {
        foreach (var definition in gameConfiguration.MiniGameDefinitions.Where(d => d.Type == type))
        {
            if (definition.GameLevel < 1 || definition.GameLevel > tierLevels.Count)
            {
                definition.IsActive = false;
                continue;
            }

            var (minimumLevel, maximumLevel) = tierLevels[definition.GameLevel - 1];
            definition.IsActive = true;
            definition.TicketItem = null;
            definition.FreeTicketItem = null;
            definition.EntranceFee = 0;
            definition.RequiresMasterClass = false;
            definition.MinimumCharacterLevel = minimumLevel;
            definition.MaximumCharacterLevel = maximumLevel;
            definition.MinimumSpecialCharacterLevel = minimumLevel;
            definition.MaximumSpecialCharacterLevel = maximumLevel;
            definition.MinimumPlayerCount = 1;
            definition.MaximumPlayerCount = MaximumPlayersPerRoom;
            definition.MapCreationPolicy = MiniGameMapCreationPolicy.OnePerParty;
            definition.EnterDuration = EntranceDuration;
            definition.GameDuration = GameDuration;
            definition.AllowParty = true;
            if (definition.Entrance?.Map is { } map)
            {
                map.ExpMultiplier = 2;
            }
        }
    }

    private static PlugInConfiguration? Find<TPlugIn>(GameConfiguration gameConfiguration)
        => gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == typeof(TPlugIn).GUID);
}
