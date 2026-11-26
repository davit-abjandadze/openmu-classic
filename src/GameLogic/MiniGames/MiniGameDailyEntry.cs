// <copyright file="MiniGameDailyEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// The daily limit of the instanced mini games: a character enters each of them once per day.
/// </summary>
/// <remarks>
/// The day is the calendar day in the time zone of the server, so the limit resets at its midnight.
/// The character keeps the day of its last entry (see <see cref="MiniGameEntryStats"/>), so no job
/// has to reset anything: an entry is possible when the stored day differs from the current one.
/// </remarks>
public static class MiniGameDailyEntry
{
    private static readonly DateTime DayZero = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    /// <summary>
    /// Gets the day number (days since 2000-01-01) of the specified point in time, in the specified time zone.
    /// </summary>
    /// <param name="timeZone">The time zone of the server.</param>
    /// <param name="utcNow">The point in time, in UTC.</param>
    /// <returns>The day number.</returns>
    public static int GetDay(TimeZoneInfo timeZone, DateTime utcNow)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timeZone);
        return (int)(local.Date - DayZero).TotalDays;
    }

    /// <summary>
    /// Determines whether the player already entered the mini game of the specified type on the current day.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="type">The type of the mini game.</param>
    /// <param name="utcNow">The current point in time, in UTC.</param>
    /// <returns><c>true</c>, if the player entered it already; otherwise, <c>false</c>.</returns>
    public static bool HasEnteredToday(Player player, MiniGameType type, DateTime utcNow)
    {
        if (MiniGameEntryStats.GetEntryDayAttribute(type) is not { } attribute
            || player.Attributes is not { } attributes)
        {
            return false;
        }

        return HasEnteredOnDay(attributes[attribute], GetDay(player.GameContext.ServerTimeZone, utcNow));
    }

    /// <summary>
    /// Determines whether the stored entry day is the specified day.
    /// </summary>
    /// <param name="storedEntryDay">The stored day of the last entry; 0 when the character never entered.</param>
    /// <param name="day">The day.</param>
    /// <returns><c>true</c>, if the stored day is the day; otherwise, <c>false</c>.</returns>
    public static bool HasEnteredOnDay(float storedEntryDay, int day) => (int)storedEntryDay == day;

    /// <summary>
    /// Remembers that the player entered the mini game of the specified type on the current day.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="type">The type of the mini game.</param>
    /// <param name="utcNow">The current point in time, in UTC.</param>
    /// <returns>
    /// <c>true</c>, if the entry was remembered; <c>false</c>, if the character class doesn't persist the
    /// attribute of the type (see <see cref="MiniGameEntryStats"/>), so the limit can't be applied.
    /// </returns>
    public static bool MarkEntered(Player player, MiniGameType type, DateTime utcNow)
    {
        return MiniGameEntryStats.GetEntryDayAttribute(type) is { } attribute
            && player.Attributes is { } attributes
            && attributes.SetStatAttribute(attribute, GetDay(player.GameContext.ServerTimeZone, utcNow));
    }

    /// <summary>
    /// Determines whether the character of the player persists the entry day of the specified type,
    /// which is required to apply the daily limit.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="type">The type of the mini game.</param>
    /// <returns><c>true</c>, if the entry day is persisted; otherwise, <c>false</c>.</returns>
    public static bool IsTracked(Player player, MiniGameType type)
    {
        return MiniGameEntryStats.GetEntryDayAttribute(type) is { } attribute
            && player.SelectedCharacter?.CharacterClass?.StatAttributes.Any(a => a.Attribute == attribute) is true;
    }
}
