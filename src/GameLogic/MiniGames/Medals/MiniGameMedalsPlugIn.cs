// <copyright file="MiniGameMedalsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Medals;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Gives silver medals for the results of the Devil Square and the Blood Castle:
/// <list type="bullet">
///   <item>Devil Square: for the 1st, 2nd and 3rd place.</item>
///   <item>Blood Castle: for the winner and the other members of its party.</item>
///   <item>Both: one for each player who killed at least a number of monsters.</item>
/// </list>
/// The silver medals are put into the inventory as items (see <see cref="MedalItems"/>) and pay at the <see cref="MedalMerchantPlugIn"/>.
/// The 1st place of the Devil Square and the winner of the Blood Castle also count as a win, see <see cref="MedalStats"/>.
/// </summary>
[PlugIn]
[Display(Name = "Medals for Devil Square and Blood Castle", Description = "Gives silver medals for the results of the Devil Square and the Blood Castle. They're put into the inventory, or added to the character when it's full, and pay at the medal merchant.")]
[Guid("3E9D4B27-6C1A-4D85-B0F3-8A2E5C7D1B10")]
public class MiniGameMedalsPlugIn : IMiniGameEndedPlugIn, IAttackableGotKilledPlugIn, ISupportCustomConfiguration<MiniGameMedalsConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    private readonly ConcurrentDictionary<(MiniGameContext Game, string Name), int> _kills = new();

    /// <inheritdoc />
    public MiniGameMedalsConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killed is Monster { SummonedBy: null }
            && (killer as Player ?? (killer as Monster)?.SummonedBy) is { CurrentMiniGame: { } game } player
            && game.Definition.Type is MiniGameType.DevilSquare or MiniGameType.BloodCastle)
        {
            this._kills.AddOrUpdate((game, player.Name), 1, (_, count) => count + 1);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask MiniGameEndedAsync(MiniGameContext miniGame, Player? winner, IReadOnlyCollection<Player> finishers)
    {
        var kills = this._kills.Where(k => k.Key.Game == miniGame).ToDictionary(k => k.Key.Name, k => k.Value);
        foreach (var key in this._kills.Keys.Where(k => k.Game == miniGame).ToList())
        {
            this._kills.TryRemove(key, out _);
        }

        if (this.Configuration is not { } configuration)
        {
            return;
        }

        var type = miniGame.Definition.Type;
        var gameWinner = GetWinner(type, miniGame.RankedFinishers, winner);
        var medals = CalculateMedals(type, miniGame.RankedFinishers, winner, finishers, kills, configuration);
        foreach (var player in medals.Keys.Append(gameWinner).OfType<Player>().Distinct())
        {
            await AwardAsync(player, type, medals.GetValueOrDefault(player), player == gameWinner).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new MiniGameMedalsConfiguration();

    /// <summary>
    /// Gets the winner of the mini game, which is the 1st place of the Devil Square or the winner of the Blood Castle.
    /// </summary>
    /// <param name="type">The type of the mini game.</param>
    /// <param name="rankedFinishers">The finishers, ordered by their rank.</param>
    /// <param name="winner">The winner, if there is one.</param>
    /// <returns>The winner, or <c>null</c>.</returns>
    internal static Player? GetWinner(MiniGameType type, IReadOnlyList<Player> rankedFinishers, Player? winner) => type switch
    {
        MiniGameType.DevilSquare => rankedFinishers.FirstOrDefault(),
        MiniGameType.BloodCastle => winner,
        _ => null,
    };

    /// <summary>
    /// Calculates the silver medals of the players.
    /// </summary>
    /// <param name="type">The type of the mini game.</param>
    /// <param name="rankedFinishers">The finishers, ordered by their rank.</param>
    /// <param name="winner">The winner, if there is one.</param>
    /// <param name="finishers">The finishers.</param>
    /// <param name="kills">The monster kills by character name.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The silver medals by player.</returns>
    internal static Dictionary<Player, int> CalculateMedals(
        MiniGameType type,
        IReadOnlyList<Player> rankedFinishers,
        Player? winner,
        IReadOnlyCollection<Player> finishers,
        IReadOnlyDictionary<string, int> kills,
        MiniGameMedalsConfiguration configuration)
    {
        var medals = new Dictionary<Player, int>();
        void Add(Player player, int silver)
        {
            if (silver > 0)
            {
                medals[player] = medals.GetValueOrDefault(player) + silver;
            }
        }

        if (type == MiniGameType.DevilSquare)
        {
            for (var i = 0; i < rankedFinishers.Count && i < 3; i++)
            {
                Add(rankedFinishers[i], i == 0 ? configuration.DevilSquareWinnerSilverMedals : configuration.DevilSquareRunnerUpSilverMedals);
            }
        }
        else if (type == MiniGameType.BloodCastle && winner is not null)
        {
            Add(winner, configuration.BloodCastleWinnerSilverMedals);
            if (winner.Party is { } party)
            {
                foreach (var member in finishers.Where(f => f != winner && f.Party == party))
                {
                    Add(member, configuration.BloodCastlePartySilverMedals);
                }
            }
        }

        if (configuration.KillsForSilverMedal > 0)
        {
            foreach (var finisher in finishers.Where(f => kills.GetValueOrDefault(f.Name) >= configuration.KillsForSilverMedal))
            {
                Add(finisher, 1);
            }
        }

        return medals;
    }

    /// <summary>
    /// Awards the silver medals to the player (see <see cref="MedalItems.AwardSilverAsync"/>) and counts the win.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="type">The type of the mini game.</param>
    /// <param name="silver">The number of silver medals.</param>
    /// <param name="isWinner">If set to <c>true</c>, the player won the mini game.</param>
    /// <returns>The task.</returns>
    internal static async ValueTask AwardAsync(Player player, MiniGameType type, int silver, bool isWinner)
    {
        if (player.Attributes is not { } attributes || (silver == 0 && !isWinner))
        {
            return;
        }

        await MedalItems.AwardSilverAsync(player, silver).ConfigureAwait(false);
        if (isWinner)
        {
            var wins = type == MiniGameType.DevilSquare ? MedalStats.DevilSquareWins : MedalStats.BloodCastleWins;
            attributes[wins] += 1;
        }

        await player.SaveProgressAsync().ConfigureAwait(false);
    }
}
