// <copyright file="HuntingJewelStats.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.HuntingRewards;

using MUnique.OpenMU.AttributeSystem;

/// <summary>
/// The attributes of a character which hold its progress of the <see cref="HuntingJewelPlugIn"/>.
/// To be persisted, they need to be stat attributes of the character classes.
/// </summary>
public static class HuntingJewelStats
{
    /// <summary>
    /// Gets the hunting seconds which were collected for the next jewel.
    /// </summary>
    public static AttributeDefinition HuntingSeconds { get; } = new(new Guid("5A1E7C33-9D2B-4F6A-8E41-7B3C2D1F0A01"), "Hunting Seconds", "The hunting seconds which were collected for the next jewel.");

    /// <summary>
    /// Gets the number of jewels which were earned on <see cref="Day"/>.
    /// </summary>
    public static AttributeDefinition JewelsOfDay { get; } = new(new Guid("5A1E7C33-9D2B-4F6A-8E41-7B3C2D1F0A02"), "Hunting Jewels Of Day", "The number of jewels which were earned for hunting on the day.");

    /// <summary>
    /// Gets the day (days since 2000-01-01 in the time zone of the server) to which <see cref="JewelsOfDay"/> belongs.
    /// </summary>
    public static AttributeDefinition Day { get; } = new(new Guid("5A1E7C33-9D2B-4F6A-8E41-7B3C2D1F0A03"), "Hunting Jewel Day", "The day to which the number of earned jewels belongs.");

    /// <summary>
    /// Gets all attributes.
    /// </summary>
    public static IReadOnlyList<AttributeDefinition> All { get; } = [HuntingSeconds, JewelsOfDay, Day];
}
