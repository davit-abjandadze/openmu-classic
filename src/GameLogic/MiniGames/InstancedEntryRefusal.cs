// <copyright file="InstancedEntryRefusal.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// The reason why a player can't enter an instanced mini game.
/// </summary>
public enum InstancedEntryRefusal
{
    /// <summary>
    /// The player can enter.
    /// </summary>
    None,

    /// <summary>
    /// The player isn't online.
    /// </summary>
    Offline,

    /// <summary>
    /// The player is dead.
    /// </summary>
    Dead,

    /// <summary>
    /// The player is too far away from the party leader.
    /// </summary>
    TooFarAway,

    /// <summary>
    /// The player is in another event, a duel or a guild war.
    /// </summary>
    InOtherEvent,

    /// <summary>
    /// The player is a player killer.
    /// </summary>
    PlayerKiller,

    /// <summary>
    /// The player entered the mini game already on this day.
    /// </summary>
    AlreadyEnteredToday,

    /// <summary>
    /// The level of the player is below every tier.
    /// </summary>
    LevelTooLow,

    /// <summary>
    /// The character doesn't persist the daily entries, so the daily limit can't be applied.
    /// </summary>
    NotTracked,
}
