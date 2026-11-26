// <copyright file="MiniGameEntryStats.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using MUnique.OpenMU.AttributeSystem;

/// <summary>
/// The attributes which hold the daily entries of a character into the instanced mini games.
/// They're persisted as stat attributes of the character, so the daily limit survives a restart of the server.
/// </summary>
public static class MiniGameEntryStats
{
    /// <summary>
    /// Gets the day (see <see cref="MiniGameDailyEntry.GetDay"/>) on which the character entered a devil square the last time.
    /// </summary>
    public static AttributeDefinition DevilSquareEntryDay { get; } = new(new Guid("ADAB99D5-D33D-4459-A079-1941DECE761A"), "Devil Square Entry Day", "The day on which the character entered a devil square the last time.");

    /// <summary>
    /// Gets the day (see <see cref="MiniGameDailyEntry.GetDay"/>) on which the character entered a blood castle the last time.
    /// </summary>
    public static AttributeDefinition BloodCastleEntryDay { get; } = new(new Guid("3F5614AB-2DC5-4E19-A59A-F77ABF609C85"), "Blood Castle Entry Day", "The day on which the character entered a blood castle the last time.");

    /// <summary>
    /// Gets all attributes.
    /// </summary>
    public static IReadOnlyList<AttributeDefinition> All { get; } = [DevilSquareEntryDay, BloodCastleEntryDay];

    /// <summary>
    /// Gets the attribute which holds the day of the last entry into the mini game of the specified type.
    /// </summary>
    /// <param name="type">The type of the mini game.</param>
    /// <returns>The attribute; or <c>null</c>, if entries of this type aren't limited per day.</returns>
    public static AttributeDefinition? GetEntryDayAttribute(MiniGameType type) => type switch
    {
        MiniGameType.DevilSquare => DevilSquareEntryDay,
        MiniGameType.BloodCastle => BloodCastleEntryDay,
        _ => null,
    };
}
