// <copyright file="MedalStats.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Medals;

using MUnique.OpenMU.AttributeSystem;

/// <summary>
/// The attributes of a character which hold its medals of the <see cref="MiniGameMedalsPlugIn"/>.
/// To be persisted, they need to be stat attributes of the character classes.
/// </summary>
/// <remarks>
/// The GUID 3E9D4B27-6C1A-4D85-B0F3-8A2E5C7D1B01 belonged to the former gold medals and must not be reused,
/// because existing databases still hold it.
/// </remarks>
public static class MedalStats
{
    /// <summary>
    /// Gets the number of silver medals.
    /// </summary>
    public static AttributeDefinition SilverMedals { get; } = new(new Guid("3E9D4B27-6C1A-4D85-B0F3-8A2E5C7D1B02"), "Silver Medals", "The number of silver medals, which are won in Devil Square and Blood Castle.");

    /// <summary>
    /// Gets the number of won Devil Squares (1st places).
    /// </summary>
    public static AttributeDefinition DevilSquareWins { get; } = new(new Guid("3E9D4B27-6C1A-4D85-B0F3-8A2E5C7D1B03"), "Devil Square Wins", "The number of won Devil Squares.");

    /// <summary>
    /// Gets the number of won Blood Castles.
    /// </summary>
    public static AttributeDefinition BloodCastleWins { get; } = new(new Guid("3E9D4B27-6C1A-4D85-B0F3-8A2E5C7D1B04"), "Blood Castle Wins", "The number of won Blood Castles.");

    /// <summary>
    /// Gets all attributes.
    /// </summary>
    public static IReadOnlyList<AttributeDefinition> All { get; } = [SilverMedals, DevilSquareWins, BloodCastleWins];
}
