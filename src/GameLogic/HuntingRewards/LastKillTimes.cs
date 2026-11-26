// <copyright file="LastKillTimes.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.HuntingRewards;

using System.Runtime.CompilerServices;

/// <summary>
/// Remembers the point in time of the last monster kill per character, to measure the hunting time between kills.
/// </summary>
/// <remarks>
/// The times are kept per character instead of per player, so that switching the character doesn't
/// count the hunting time of the previous one. They are kept in a <see cref="ConditionalWeakTable{TKey, TValue}"/>,
/// which doesn't keep the characters alive after they left the game.
/// </remarks>
internal sealed class LastKillTimes
{
    private readonly ConditionalWeakTable<Character, StrongBox<DateTime>> _lastKills = new();

    /// <summary>
    /// Remembers the kill of the character at the specified point in time and returns the time since its previous kill.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="now">The point in time of the kill.</param>
    /// <returns>The time since the previous kill, or <see cref="TimeSpan.Zero"/> for the first kill.</returns>
    public TimeSpan GetTimeSinceLastKill(Character character, DateTime now)
    {
        var lastKill = this._lastKills.GetValue(character, _ => new StrongBox<DateTime>(now));
        lock (lastKill)
        {
            var previous = lastKill.Value;
            lastKill.Value = now;
            return now > previous ? now - previous : TimeSpan.Zero;
        }
    }
}
