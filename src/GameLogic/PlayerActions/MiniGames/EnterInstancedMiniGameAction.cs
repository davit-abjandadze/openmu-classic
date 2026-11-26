// <copyright file="EnterInstancedMiniGameAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.MiniGames;

using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.GuildWar;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Player action which enters an instanced mini game (see <see cref="IInstancedMiniGameSlotProvider"/>):
/// a solo player or the leader with its whole party enters an own instance of the tier of its level,
/// once per day and mini game type.
/// </summary>
public class EnterInstancedMiniGameAction
{
    /// <summary>
    /// The maximum number of refused party members which are listed to the leader.
    /// </summary>
    private const int MaximumListedRefusals = 5;

    /// <summary>
    /// Tries to enter the instanced mini game of the specified type.
    /// </summary>
    /// <param name="player">The player who requested to enter.</param>
    /// <param name="type">The type of the mini game.</param>
    /// <returns>
    /// <c>true</c>, if the mini game runs in instances and the request was handled (successfully or not);
    /// <c>false</c>, if the mini game doesn't run in instances.
    /// </returns>
    public async ValueTask<bool> TryEnterAsync(Player player, MiniGameType type)
    {
        var gameContext = player.GameContext;
        if (gameContext.PlugInManager.GetStrategy<MiniGameType, IPeriodicMiniGameStartPlugIn>(type) is not IInstancedMiniGameSlotProvider slotProvider
            || !slotProvider.IsInstanced(gameContext)
            || player.SelectedCharacter is null)
        {
            return false;
        }

        var tiers = gameContext.Configuration.MiniGameDefinitions
            .Where(d => d.Type == type && d.MapCreationPolicy == MiniGameMapCreationPolicy.OnePerParty && d.IsAvailable())
            .OrderBy(d => d.GameLevel)
            .ToList();
        var eventName = tiers[0].Name;

        if (player.CurrentMiniGame is not null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InstancedEventAlreadyInEvent)).ConfigureAwait(false);
            return true;
        }

        if (await this.TryReenterAsync(player, type).ConfigureAwait(false))
        {
            return true;
        }

        if (slotProvider.GetOpenSlot(gameContext) is not { } slot)
        {
            await ShowNotOpenAsync(player, slotProvider, eventName).ConfigureAwait(false);
            return true;
        }

        if (player.Party is { } party && !ReferenceEquals(party.PartyMaster, player))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InstancedEventOnlyPartyLeaderFormat), eventName).ConfigureAwait(false);
            return true;
        }

        var members = player.Party?.PartyList.OfType<Player>().ToList() ?? [player];
        var bots = members.OfType<OfflinePlayer>().Where(m => m.Account?.IsBot == true).ToList();
        var humans = members.Except(bots).ToList();

        var utcNow = DateTime.UtcNow;
        var distance = slot.Configuration.EffectiveMaximumPartyDistance;
        var candidates = humans.Select(m => CreateCandidate(m, player, tiers, type, distance, utcNow)).ToList();
        var refusals = InstancedMiniGameEntryRules.Validate(candidates);
        if (refusals.Count > 0)
        {
            await ShowRefusalsAsync(player, eventName, refusals).ConfigureAwait(false);
            return true;
        }

        // The bots of the party are not checked like players (they follow the leader in on their own),
        // but they belong to the party, so a low level bot lowers its tier, too.
        var botTiers = bots
            .Select(b => InstancedMiniGameEntryRules.SelectTier(tiers, (int)(b.Attributes?[Stats.Level] ?? 0), b.SelectedCharacter?.IsSpecialCharacter() ?? false))
            .OfType<MiniGameDefinition>();
        var tier = InstancedMiniGameEntryRules.SelectPartyTier(candidates) is { } playerTier
            ? botTiers.Append(playerTier).MinBy(t => t.GameLevel)
            : null;
        if (tier?.Entrance is not { } entrance)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MiniGameEnterFailed)).ConfigureAwait(false);
            return true;
        }

        var groupSize = humans.Count + bots.Count;
        var memberIds = members.Select(m => m.SelectedCharacter?.Id).OfType<Guid>().ToList();
        MiniGameContext game;
        using (await slot.EntryLock.LockAsync().ConfigureAwait(false))
        {
            var isShared = slot.Configuration.IsSharedPerTier;
            var sharedGame = isShared ? FindSharedInstance(gameContext, slot, tier, groupSize) : null;
            if (sharedGame is null && !slot.TryReserveInstance())
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InstancedEventFullFormat), eventName).ConfigureAwait(false);
                return true;
            }

            // A shared instance is scaled by all of its players when its game starts; until then, by the first group.
            game = sharedGame ?? await gameContext.MiniGames.CreateInstanceAsync(
                    tier,
                    new InstancedMiniGameSettings(slot, memberIds, PartySizeScaling.Find(slot.Configuration.PartySizeScalings, groupSize), isShared))
                .ConfigureAwait(false);

            if (!await this.TryEnterAllAsync(player, game, humans, eventName).ConfigureAwait(false))
            {
                if (sharedGame is null)
                {
                    slot.ReleaseInstance();
                    await game.DisposeAsync().ConfigureAwait(false);
                }

                return true;
            }

            // Members are only added when all of them entered, so that a refused party can't use the
            // re-entry of members to enter without using up its attempt.
            game.InstanceSettings?.AddMembers(memberIds);

            // The attempt is used up as soon as the players entered, and saved right away,
            // so that neither leaving the game nor a restart of the server gives it back.
            foreach (var member in humans)
            {
                MiniGameDailyEntry.MarkEntered(member, type, utcNow);
            }
        }

        player.Logger.LogInformation(
            "{Leader} entered {Game} with {Count} player(s) ({Members})",
            player.Name,
            game,
            groupSize,
            string.Join(", ", members.Select(m => m.Name)));

        var secondsUntilStart = (int)Math.Max(0, Math.Ceiling((slot.EntranceClosesUtc - DateTime.UtcNow).TotalSeconds));
        foreach (var member in humans)
        {
            await member.SaveProgressAsync().ConfigureAwait(false);
            await EnterMiniGameAction.WarpIntoAsync(member, entrance).ConfigureAwait(false);
            await member.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InstancedEventEnteredFormat), tier.Name, secondsUntilStart).ConfigureAwait(false);
        }

        Bots.BotMiniGameHandler.BringPartyBotsAlong(player, bots, tier, game);
        return true;
    }

    /// <summary>
    /// Finds the shared instance of the tier in the slot, which is still open and has room for the group.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="slot">The slot.</param>
    /// <param name="tier">The tier.</param>
    /// <param name="groupSize">The number of players of the group, including its bots.</param>
    /// <returns>The shared instance; or <c>null</c>, if a new one has to be created.</returns>
    private static MiniGameContext? FindSharedInstance(IGameContext gameContext, InstancedMiniGameSlot slot, MiniGameDefinition tier, int groupSize)
    {
        return gameContext.MiniGames.GetRunningMiniGames(tier.Type)
            .FirstOrDefault(g => g.InstanceSettings is { IsShared: true } settings
                                 && ReferenceEquals(settings.Slot, slot)
                                 && Equals(g.Definition, tier)
                                 && g.State == MiniGameState.Open
                                 && g.PlayerCount + groupSize <= tier.MaximumPlayerCount);
    }

    private static InstancedEntryCandidate CreateCandidate(Player member, Player leader, IReadOnlyList<MiniGameDefinition> tiers, MiniGameType type, int maximumDistance, DateTime utcNow)
    {
        var character = member.SelectedCharacter;
        var level = (int)(member.Attributes?[Stats.Level] ?? 0);
        var isOnline = member is not OfflinePlayer
            && character is not null
            && member.PlayerState.CurrentState == PlayerState.EnteredWorld;
        var isNearLeader = ReferenceEquals(member, leader)
            || (member.CurrentMap is { } map && ReferenceEquals(map, leader.CurrentMap) && member.IsInRange(leader, maximumDistance));
        var isInOtherEvent = member.CurrentMiniGame is not null
            || member.DuelRoom is not null
            || member.GuildWarContext is { State: GuildWarState.Started or GuildWarState.Requested };
        var isPlayerKiller = character?.State >= HeroState.PlayerKiller1stStage
            && !tiers.All(t => t.ArePlayerKillersAllowedToEnter);

        return new InstancedEntryCandidate(
            member.Name,
            level,
            isOnline,
            member.IsAlive,
            isNearLeader,
            isInOtherEvent,
            isPlayerKiller,
            MiniGameDailyEntry.HasEnteredToday(member, type, utcNow),
            MiniGameDailyEntry.IsTracked(member, type),
            InstancedMiniGameEntryRules.SelectTier(tiers, level, character?.IsSpecialCharacter() ?? false));
    }

    private static async ValueTask ShowNotOpenAsync(Player player, IInstancedMiniGameSlotProvider slotProvider, LocalizedString eventName)
    {
        if (slotProvider.GetNextSlotStartUtc(player.GameContext) is not { } nextStartUtc)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MiniGameNotOpenFormat), eventName).ConfigureAwait(false);
            return;
        }

        var localStart = TimeZoneInfo.ConvertTimeFromUtc(nextStartUtc, player.GameContext.ServerTimeZone);
        var minutes = (int)Math.Ceiling((nextStartUtc - DateTime.UtcNow).TotalMinutes);
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InstancedEventNotOpenFormat), eventName, localStart.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture), minutes).ConfigureAwait(false);
    }

    private static async ValueTask ShowRefusalsAsync(Player leader, LocalizedString eventName, IReadOnlyList<(string Name, InstancedEntryRefusal Reason)> refusals)
    {
        await leader.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InstancedEventNobodyEnteredFormat), eventName).ConfigureAwait(false);
        foreach (var (name, reason) in refusals.Take(MaximumListedRefusals))
        {
            var reasonText = leader.GetLocalizedMessage(GetReasonKey(reason));
            await leader.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InstancedEventMemberRefusedFormat), name, reasonText).ConfigureAwait(false);
        }
    }

    private static string GetReasonKey(InstancedEntryRefusal reason) => reason switch
    {
        InstancedEntryRefusal.Offline => nameof(PlayerMessage.InstancedEventReasonOffline),
        InstancedEntryRefusal.Dead => nameof(PlayerMessage.InstancedEventReasonDead),
        InstancedEntryRefusal.TooFarAway => nameof(PlayerMessage.InstancedEventReasonTooFarAway),
        InstancedEntryRefusal.InOtherEvent => nameof(PlayerMessage.InstancedEventReasonInOtherEvent),
        InstancedEntryRefusal.PlayerKiller => nameof(PlayerMessage.InstancedEventReasonPlayerKiller),
        InstancedEntryRefusal.AlreadyEnteredToday => nameof(PlayerMessage.InstancedEventReasonAlreadyEnteredToday),
        InstancedEntryRefusal.LevelTooLow => nameof(PlayerMessage.InstancedEventReasonLevelTooLow),
        InstancedEntryRefusal.NotTracked => nameof(PlayerMessage.InstancedEventReasonNotTracked),
        _ => nameof(PlayerMessage.MiniGameEnterFailed),
    };

    /// <summary>
    /// Lets a member of a running instance enter it again, e.g. after it died or left it.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="type">The type of the mini game.</param>
    /// <returns><c>true</c>, if the player is a member of a running instance; otherwise, <c>false</c>.</returns>
    private async ValueTask<bool> TryReenterAsync(Player player, MiniGameType type)
    {
        var game = player.GameContext.MiniGames.GetRunningMiniGames(type)
            .FirstOrDefault(g => g.InstanceSettings?.IsMember(player) is true
                                 && g.State is MiniGameState.Open or MiniGameState.Closed or MiniGameState.Playing);
        if (game?.Definition.Entrance is not { } entrance)
        {
            return false;
        }

        var result = await game.TryEnterAsync(player).ConfigureAwait(false);
        if (result != EnterResult.Success)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MiniGameEnterFailed)).ConfigureAwait(false);
            return true;
        }

        await EnterMiniGameAction.WarpIntoAsync(player, entrance).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Lets all players enter the instance, or nobody: when one of them can't enter, e.g. because
    /// another account of its IP address entered the event already, the others are taken back.
    /// </summary>
    /// <param name="leader">The leader which requested to enter.</param>
    /// <param name="game">The instance.</param>
    /// <param name="players">The players.</param>
    /// <param name="eventName">The name of the mini game.</param>
    /// <returns><c>true</c>, if all entered; otherwise, <c>false</c>.</returns>
    private async ValueTask<bool> TryEnterAllAsync(Player leader, MiniGameContext game, IReadOnlyList<Player> players, LocalizedString eventName)
    {
        var entered = new List<Player>(players.Count);
        foreach (var player in players)
        {
            if (await game.TryEnterAsync(player).ConfigureAwait(false) == EnterResult.Success)
            {
                entered.Add(player);
                continue;
            }

            foreach (var enteredPlayer in entered)
            {
                await game.CancelEntryAsync(enteredPlayer).ConfigureAwait(false);
            }

            await leader.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InstancedEventNobodyEnteredFormat), eventName).ConfigureAwait(false);
            await leader.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InstancedEventMemberRefusedFormat), player.Name, leader.GetLocalizedMessage(nameof(PlayerMessage.InstancedEventReasonEntryFailed))).ConfigureAwait(false);
            return false;
        }

        return true;
    }
}
