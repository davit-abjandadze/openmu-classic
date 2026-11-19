// <copyright file="MiniGameMedalsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Medals;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Gives medals for the results of the Devil Square and the Blood Castle:
/// <list type="bullet">
///   <item>Devil Square: a gold medal for the 1st place, silver medals for the 2nd and 3rd place.</item>
///   <item>Blood Castle: a gold medal for the winner, a silver medal for the other members of its party.</item>
///   <item>Both: a silver medal for each player who killed at least a number of monsters.</item>
/// </list>
/// The medals can be exchanged with the /medals command.
/// </summary>
[PlugIn]
[Display(Name = "Medals for Devil Square and Blood Castle", Description = "Gives gold and silver medals for the results of the Devil Square and the Blood Castle, which can be exchanged with /medals.")]
[Guid("3E9D4B27-6C1A-4D85-B0F3-8A2E5C7D1B10")]
public class MiniGameMedalsPlugIn : IMiniGameEndedPlugIn, IAttackableGotKilledPlugIn, ISupportCustomConfiguration<MiniGameMedalsConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    private readonly ConcurrentDictionary<(MiniGameContext Game, string Name), int> _kills = new();

    /// <inheritdoc />
    public MiniGameMedalsConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the active instance of the plugin.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The active instance, or <c>null</c>.</returns>
    public static MiniGameMedalsPlugIn? GetActive(IGameContext gameContext)
        => gameContext.PlugInManager.GetActivePlugInsOf<IMiniGameEndedPlugIn>().OfType<MiniGameMedalsPlugIn>().FirstOrDefault();

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

        var medals = CalculateMedals(miniGame.Definition.Type, miniGame.RankedFinishers, winner, finishers, kills, configuration);
        foreach (var (player, (gold, silver)) in medals)
        {
            await AwardAsync(player, miniGame.Definition.Type, gold, silver).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => MiniGameMedalsConfiguration.CreateDefault();

    /// <summary>
    /// Calculates the medals of the players.
    /// </summary>
    /// <param name="type">The type of the mini game.</param>
    /// <param name="rankedFinishers">The finishers, ordered by their rank.</param>
    /// <param name="winner">The winner, if there is one.</param>
    /// <param name="finishers">The finishers.</param>
    /// <param name="kills">The monster kills by character name.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The gold and silver medals by player.</returns>
    internal static Dictionary<Player, (int Gold, int Silver)> CalculateMedals(
        MiniGameType type,
        IReadOnlyList<Player> rankedFinishers,
        Player? winner,
        IReadOnlyCollection<Player> finishers,
        IReadOnlyDictionary<string, int> kills,
        MiniGameMedalsConfiguration configuration)
    {
        var medals = new Dictionary<Player, (int Gold, int Silver)>();
        void Add(Player player, int gold, int silver)
        {
            var (g, s) = medals.GetValueOrDefault(player);
            medals[player] = (g + gold, s + silver);
        }

        if (type == MiniGameType.DevilSquare)
        {
            for (var i = 0; i < rankedFinishers.Count && i < 3; i++)
            {
                if (i == 0)
                {
                    Add(rankedFinishers[i], 1, 0);
                }
                else
                {
                    Add(rankedFinishers[i], 0, configuration.DevilSquareRunnerUpSilverMedals);
                }
            }
        }
        else if (type == MiniGameType.BloodCastle && winner is not null)
        {
            Add(winner, 1, 0);
            if (winner.Party is { } party)
            {
                foreach (var member in finishers.Where(f => f != winner && f.Party == party))
                {
                    Add(member, 0, configuration.BloodCastlePartySilverMedals);
                }
            }
        }

        if (configuration.KillsForSilverMedal > 0)
        {
            foreach (var finisher in finishers.Where(f => kills.GetValueOrDefault(f.Name) >= configuration.KillsForSilverMedal))
            {
                Add(finisher, 0, 1);
            }
        }

        return medals;
    }

    private static async ValueTask AwardAsync(Player player, MiniGameType type, int gold, int silver)
    {
        if (player.Attributes is not { } attributes || (gold == 0 && silver == 0))
        {
            return;
        }

        attributes[MedalStats.GoldMedals] += gold;
        attributes[MedalStats.SilverMedals] += silver;
        if (gold > 0)
        {
            var wins = type == MiniGameType.DevilSquare ? MedalStats.DevilSquareWins : MedalStats.BloodCastleWins;
            attributes[wins] += 1;
        }

        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalsAwarded), gold, silver).ConfigureAwait(false);
        await player.SaveProgressAsync().ConfigureAwait(false);
    }
}
