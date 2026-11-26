// <copyright file="TeamVsTeamState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.TeamVsTeam;

using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// The state of the <see cref="TeamVsTeamPlugIn"/> on a game server.
/// </summary>
public class TeamVsTeamState : PeriodicTaskGameServerState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeamVsTeamState"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    public TeamVsTeamState(IGameContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Gets the lock for the collections of this state.
    /// </summary>
    public object SyncRoot { get; } = new();

    /// <summary>
    /// Gets the object which identifies the current run of the event. It's renewed when the registration opens.
    /// </summary>
    public object Run { get; private set; } = new();

    /// <summary>
    /// Gets the registered players.
    /// </summary>
    public List<Player> Registered { get; } = [];

    /// <summary>
    /// Gets the team (0 or 1) of the players of the running battle.
    /// </summary>
    public Dictionary<Player, int> Teams { get; } = [];

    /// <summary>
    /// Gets the kills of the players of the running battle.
    /// </summary>
    public Dictionary<Player, int> Kills { get; } = [];

    /// <inheritdoc />
    public override string Description => "Team vs Team";

    /// <summary>
    /// Starts a new run of the event, see <see cref="Run"/>.
    /// </summary>
    public void StartNewRun()
    {
        this.Run = new object();
    }

    /// <summary>
    /// Gets the team of the player in the running battle.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The team, or <c>null</c> if the player doesn't fight in the battle.</returns>
    public int? GetTeam(Player player)
    {
        lock (this.SyncRoot)
        {
            return this.Teams.TryGetValue(player, out var team) ? team : null;
        }
    }
}
