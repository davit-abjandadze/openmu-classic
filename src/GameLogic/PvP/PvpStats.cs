// <copyright file="PvpStats.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PvP;

using MUnique.OpenMU.AttributeSystem;

/// <summary>
/// The attributes of a character which hold its PvP statistics of the <see cref="PvpSpotsPlugIn"/>.
/// To be persisted, they need to be stat attributes of the character classes.
/// </summary>
public static class PvpStats
{
    /// <summary>
    /// Gets the number of the week to which the weekly values belong, see <see cref="PvpSpotsConfiguration.GetWeekNumber"/>.
    /// </summary>
    public static AttributeDefinition Week { get; } = new(new Guid("8E3F2C61-0A4B-4E2D-9B57-1C6D8A0F7E01"), "PvP Week", "The number of the week to which the weekly PvP values belong.");

    /// <summary>
    /// Gets the prestige of the current week.
    /// </summary>
    public static AttributeDefinition WeeklyPrestige { get; } = new(new Guid("8E3F2C61-0A4B-4E2D-9B57-1C6D8A0F7E02"), "PvP Weekly Prestige", "The PvP prestige of the current week.");

    /// <summary>
    /// Gets the kills of the current week.
    /// </summary>
    public static AttributeDefinition WeeklyKills { get; } = new(new Guid("8E3F2C61-0A4B-4E2D-9B57-1C6D8A0F7E03"), "PvP Weekly Kills", "The PvP kills of the current week.");

    /// <summary>
    /// Gets the deaths of the current week.
    /// </summary>
    public static AttributeDefinition WeeklyDeaths { get; } = new(new Guid("8E3F2C61-0A4B-4E2D-9B57-1C6D8A0F7E04"), "PvP Weekly Deaths", "The PvP deaths of the current week.");

    /// <summary>
    /// Gets the assists of the current week.
    /// </summary>
    public static AttributeDefinition WeeklyAssists { get; } = new(new Guid("8E3F2C61-0A4B-4E2D-9B57-1C6D8A0F7E05"), "PvP Weekly Assists", "The PvP assists of the current week.");

    /// <summary>
    /// Gets the prestige of all weeks.
    /// </summary>
    public static AttributeDefinition TotalPrestige { get; } = new(new Guid("8E3F2C61-0A4B-4E2D-9B57-1C6D8A0F7E06"), "PvP Total Prestige", "The PvP prestige of all weeks.");

    /// <summary>
    /// Gets the kills of all weeks.
    /// </summary>
    public static AttributeDefinition TotalKills { get; } = new(new Guid("8E3F2C61-0A4B-4E2D-9B57-1C6D8A0F7E07"), "PvP Total Kills", "The PvP kills of all weeks.");

    /// <summary>
    /// Gets the deaths of all weeks.
    /// </summary>
    public static AttributeDefinition TotalDeaths { get; } = new(new Guid("8E3F2C61-0A4B-4E2D-9B57-1C6D8A0F7E08"), "PvP Total Deaths", "The PvP deaths of all weeks.");

    /// <summary>
    /// Gets the assists of all weeks.
    /// </summary>
    public static AttributeDefinition TotalAssists { get; } = new(new Guid("8E3F2C61-0A4B-4E2D-9B57-1C6D8A0F7E09"), "PvP Total Assists", "The PvP assists of all weeks.");

    /// <summary>
    /// Gets all PvP attributes.
    /// </summary>
    public static IReadOnlyList<AttributeDefinition> All { get; } =
    [
        Week, WeeklyPrestige, WeeklyKills, WeeklyDeaths, WeeklyAssists, TotalPrestige, TotalKills, TotalDeaths, TotalAssists,
    ];

    /// <summary>
    /// Gets the weekly attributes, which are reset when a new week starts.
    /// </summary>
    public static IReadOnlyList<AttributeDefinition> Weekly { get; } = [WeeklyPrestige, WeeklyKills, WeeklyDeaths, WeeklyAssists];
}
