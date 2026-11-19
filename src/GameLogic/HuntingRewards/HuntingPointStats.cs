// <copyright file="HuntingPointStats.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.HuntingRewards;

using MUnique.OpenMU.AttributeSystem;

/// <summary>
/// The attributes of a character which hold its hunting points of the <see cref="HuntingPointsPlugIn"/>.
/// To be persisted, they need to be stat attributes of the character classes.
/// </summary>
public static class HuntingPointStats
{
    /// <summary>
    /// Gets the hunting points, which can be exchanged.
    /// </summary>
    public static AttributeDefinition Points { get; } = new(new Guid("5A1E7C33-9D2B-4F6A-8E41-7B3C2D1F0A21"), "Hunting Points", "The hunting points, which can be exchanged for items.");

    /// <summary>
    /// Gets the hunting seconds which were collected for the next point.
    /// </summary>
    public static AttributeDefinition PendingSeconds { get; } = new(new Guid("5A1E7C33-9D2B-4F6A-8E41-7B3C2D1F0A22"), "Hunting Point Seconds", "The hunting seconds which were collected for the next hunting point.");

    /// <summary>
    /// Gets the hunting seconds which counted on <see cref="Day"/>.
    /// </summary>
    public static AttributeDefinition SecondsOfDay { get; } = new(new Guid("5A1E7C33-9D2B-4F6A-8E41-7B3C2D1F0A23"), "Hunting Point Seconds Of Day", "The hunting seconds which counted for hunting points on the day.");

    /// <summary>
    /// Gets the day (days since 2000-01-01 in the time zone of the server) to which <see cref="SecondsOfDay"/> belongs.
    /// </summary>
    public static AttributeDefinition Day { get; } = new(new Guid("5A1E7C33-9D2B-4F6A-8E41-7B3C2D1F0A24"), "Hunting Point Day", "The day to which the counted hunting seconds belong.");

    /// <summary>
    /// Gets all attributes.
    /// </summary>
    public static IReadOnlyList<AttributeDefinition> All { get; } = [Points, PendingSeconds, SecondsOfDay, Day];
}
