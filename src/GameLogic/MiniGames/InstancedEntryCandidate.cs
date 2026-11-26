// <copyright file="InstancedEntryCandidate.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// The state of a player which is relevant to enter an instanced mini game.
/// </summary>
/// <param name="Name">The name of the character.</param>
/// <param name="Level">The level of the character.</param>
/// <param name="IsOnline">If set to <c>true</c>, the player is online.</param>
/// <param name="IsAlive">If set to <c>true</c>, the player is alive.</param>
/// <param name="IsNearLeader">If set to <c>true</c>, the player is near the party leader, or is the leader.</param>
/// <param name="IsInOtherEvent">If set to <c>true</c>, the player is in another event, a duel or a guild war.</param>
/// <param name="IsPlayerKiller">If set to <c>true</c>, the player is a player killer who isn't allowed to enter.</param>
/// <param name="HasEnteredToday">If set to <c>true</c>, the player entered the mini game already on this day.</param>
/// <param name="IsDailyLimitTracked">If set to <c>true</c>, the character persists its daily entries.</param>
/// <param name="Tier">The tier of the character by its level; or <c>null</c>, if its level is below every tier.</param>
public sealed record InstancedEntryCandidate(
    string Name,
    int Level,
    bool IsOnline,
    bool IsAlive,
    bool IsNearLeader,
    bool IsInOtherEvent,
    bool IsPlayerKiller,
    bool HasEnteredToday,
    bool IsDailyLimitTracked,
    MiniGameDefinition? Tier);
