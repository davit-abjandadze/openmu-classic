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
/// The events of the classic server: one free Devil Square and one free Blood Castle room for everyone,
/// joined with /joinds and /joinbc, once each night, the Golden Invasion and the Team vs Team after them.
/// </summary>
public static class ClassicEvents
{
    /// <summary>
    /// The level of the only active Devil Square.
    /// </summary>
    public const byte DevilSquareLevel = 4;

    /// <summary>
    /// The level of the only active Blood Castle.
    /// </summary>
    public const byte BloodCastleLevel = 5;

    /// <summary>
    /// The start time of the Devil Square, in the time zone of the server.
    /// </summary>
    public static readonly TimeOnly DevilSquareTime = new(21, 0);

    /// <summary>
    /// The start time of the Blood Castle, in the time zone of the server.
    /// </summary>
    public static readonly TimeOnly BloodCastleTime = new(22, 0);

    /// <summary>
    /// The start time of the Golden Invasion, in the time zone of the server.
    /// </summary>
    public static readonly TimeOnly GoldenInvasionTime = new(23, 0);

    /// <summary>
    /// The start time of the registration of the Team vs Team, in the time zone of the server.
    /// </summary>
    public static readonly TimeOnly TeamVsTeamTime = new(23, 30);

    /// <summary>
    /// Adapts the mini game definitions: only one room per event, free, for all levels, with double experience.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    public static void ApplyToMiniGames(GameConfiguration gameConfiguration)
    {
        Adapt(gameConfiguration, MiniGameType.DevilSquare, DevilSquareLevel, TimeSpan.FromMinutes(8));
        Adapt(gameConfiguration, MiniGameType.BloodCastle, BloodCastleLevel, TimeSpan.FromMinutes(15));
    }

    /// <summary>
    /// Configures the timetables of the event plugins.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void ApplyToPlugIns(GameConfiguration gameConfiguration, ReferenceHandler referenceHandler)
    {
        if (Find<DevilSquareStartPlugIn>(gameConfiguration) is { } devilSquare)
        {
            var configuration = DevilSquareStartConfiguration.Default;
            configuration.Timetable = [DevilSquareTime];
            configuration.TaskDuration = TimeSpan.FromMinutes(25);
            configuration.EntranceOpenedMessage = "Devil Square is open for {0} minute(s)! Join with /joinds.";
            devilSquare.IsActive = true;
            devilSquare.SetConfiguration(configuration, referenceHandler);
        }

        if (Find<BloodCastleStartPlugIn>(gameConfiguration) is { } bloodCastle)
        {
            var configuration = BloodCastleStartConfiguration.Default;
            configuration.Timetable = [BloodCastleTime];
            configuration.TaskDuration = TimeSpan.FromMinutes(30);
            configuration.EntranceOpenedMessage = "Blood Castle is open for {0} minute(s)! Join with /joinbc.";
            bloodCastle.IsActive = true;
            bloodCastle.SetConfiguration(configuration, referenceHandler);
        }

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

    private static void Adapt(GameConfiguration gameConfiguration, MiniGameType type, byte level, TimeSpan gameDuration)
    {
        foreach (var definition in gameConfiguration.MiniGameDefinitions.Where(d => d.Type == type))
        {
            if (definition.GameLevel != level)
            {
                definition.IsActive = false;
                continue;
            }

            definition.IsActive = true;
            definition.TicketItem = null;
            definition.FreeTicketItem = null;
            definition.EntranceFee = 0;
            definition.MinimumCharacterLevel = 10;
            definition.MaximumCharacterLevel = 400;
            definition.MinimumSpecialCharacterLevel = 10;
            definition.MaximumSpecialCharacterLevel = 400;
            definition.MinimumPlayerCount = 3;
            definition.MaximumPlayerCount = 50;
            definition.MapCreationPolicy = MiniGameMapCreationPolicy.Shared;
            definition.EnterDuration = TimeSpan.FromMinutes(10);
            definition.GameDuration = gameDuration;
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
