// <copyright file="TeamVsTeamConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.TeamVsTeam;

using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// The configuration of the <see cref="TeamVsTeamPlugIn"/>.
/// The <see cref="PeriodicTaskConfiguration.PreStartMessageDelay"/> is the registration time,
/// the <see cref="PeriodicTaskConfiguration.TaskDuration"/> the duration of the battle.
/// </summary>
public class TeamVsTeamConfiguration : PeriodicTaskConfiguration
{
    /// <summary>
    /// Gets or sets the minimum number of registered players for the battle to start.
    /// </summary>
    [Display(Name = "Minimum players")]
    public int MinimumPlayers { get; set; } = 4;

    /// <summary>
    /// Gets or sets the number of the map where the battle takes place.
    /// </summary>
    [Display(Name = "Map number", Description = "The map where the battle takes place, e.g. 6 for the Arena.")]
    public short MapNumber { get; set; } = 6;

    /// <summary>
    /// Gets or sets the points for the best player (MVP) of the winning team.
    /// </summary>
    [Display(Name = "Points for the MVP")]
    public int MostValuablePlayerPoints { get; set; } = 30;

    /// <summary>
    /// Gets or sets the points for the other players of the winning team.
    /// </summary>
    [Display(Name = "Points for the winning team")]
    public int WinnerPoints { get; set; } = 10;

    /// <summary>
    /// Gets the default configuration: daily at 23:30, 5 minutes registration and 10 minutes battle.
    /// </summary>
    public static TeamVsTeamConfiguration Default => new()
    {
        PreStartMessageDelay = TimeSpan.FromMinutes(5),
        TaskDuration = TimeSpan.FromMinutes(10),
        Timetable = [new TimeOnly(23, 30)],
    };
}
