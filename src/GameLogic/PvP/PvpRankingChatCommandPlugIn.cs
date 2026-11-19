// <copyright file="PvpRankingChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PvP;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The chat command which shows the PvP ranking of the current week, or with "all" of all weeks,
/// and the own values of the player.
/// </summary>
[Guid("2D7E5B1A-4F3C-4E8B-A6D2-9C1F0E3B7A53")]
[PlugIn]
[Display(Name = "Chat command: /ranking", Description = "Shows the PvP ranking of this week, or with '/ranking all' of all weeks.")]
[ChatCommandHelp(Command)]
public class PvpRankingChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/ranking";

    private static readonly IReadOnlyList<Guid> WeeklyIds = [PvpStats.WeeklyPrestige.Id, PvpStats.WeeklyKills.Id, PvpStats.WeeklyDeaths.Id, PvpStats.WeeklyAssists.Id];

    private static readonly IReadOnlyList<Guid> TotalIds = [PvpStats.TotalPrestige.Id, PvpStats.TotalKills.Id, PvpStats.TotalDeaths.Id, PvpStats.TotalAssists.Id];

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (PvpSpotsPlugIn.GetActive(player.GameContext)?.Configuration is not { } configuration)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.PvpNotActive)).ConfigureAwait(false);
            return;
        }

        var isTotal = command.Trim().EndsWith(" all", StringComparison.OrdinalIgnoreCase);
        var ids = isTotal ? TotalIds : WeeklyIds;
        var now = DateTime.UtcNow;

        // Ensure that the own values are current and saved, so that the ranking includes them.
        PvpSpotsPlugIn.EnsureCurrentWeek(player, configuration, now);

        IReadOnlyList<CharacterAttributeRankingEntry> ranking;
        using (var context = player.GameContext.PersistenceContextProvider.CreateNewPlayerContext(player.GameContext.Configuration))
        {
            ranking = await context.GetCharacterAttributeRankingAsync(
                    ids[0],
                    isTotal ? null : PvpStats.Week.Id,
                    configuration.GetWeekNumber(now),
                    ids,
                    Math.Max(1, configuration.RankingSize))
                .ConfigureAwait(false);
        }

        await player.ShowLocalizedBlueMessageAsync(isTotal ? nameof(PlayerMessage.PvpRankingTotalHeader) : nameof(PlayerMessage.PvpRankingWeekHeader)).ConfigureAwait(false);
        if (ranking.Count == 0)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.PvpRankingEmpty)).ConfigureAwait(false);
        }

        var position = 1;
        foreach (var entry in ranking)
        {
            await player.ShowLocalizedBlueMessageAsync(
                    nameof(PlayerMessage.PvpRankingEntry),
                    position++,
                    entry.Name,
                    GetClassName(player, entry.CharacterClassId),
                    (int)entry.Values[0],
                    (int)entry.Values[1],
                    (int)entry.Values[2],
                    (int)entry.Values[3])
                .ConfigureAwait(false);
        }

        if (player.Attributes is { } attributes)
        {
            var own = isTotal
                ? new[] { PvpStats.TotalPrestige, PvpStats.TotalKills, PvpStats.TotalDeaths, PvpStats.TotalAssists }
                : new[] { PvpStats.WeeklyPrestige, PvpStats.WeeklyKills, PvpStats.WeeklyDeaths, PvpStats.WeeklyAssists };
            await player.ShowLocalizedBlueMessageAsync(
                    nameof(PlayerMessage.PvpOwnStats),
                    (int)attributes[own[0]],
                    (int)attributes[own[1]],
                    (int)attributes[own[2]],
                    (int)attributes[own[3]])
                .ConfigureAwait(false);
        }
    }

    private static string GetClassName(Player player, Guid? characterClassId)
    {
        var characterClass = player.GameContext.Configuration.CharacterClasses.FirstOrDefault(c => c.GetId() == characterClassId);
        return characterClass?.Name.GetTranslation(player.Culture) ?? string.Empty;
    }
}
