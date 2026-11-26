// <copyright file="IInstancedMiniGameSlotProvider.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// Provides the time slots of an instanced mini game.
/// It's implemented by the start plugin of the mini game.
/// </summary>
public interface IInstancedMiniGameSlotProvider
{
    /// <summary>
    /// Determines whether the mini game runs in instances on the game context,
    /// i.e. it has active definitions with <see cref="MiniGameMapCreationPolicy.OnePerParty"/>.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns><c>true</c>, if the mini game runs in instances; otherwise, <c>false</c>.</returns>
    bool IsInstanced(IGameContext gameContext);

    /// <summary>
    /// Gets the slot whose entrance is currently open.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The slot whose entrance is open; or <c>null</c>.</returns>
    InstancedMiniGameSlot? GetOpenSlot(IGameContext gameContext);

    /// <summary>
    /// Gets the next start of a slot.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The next start, in UTC; or <c>null</c>, if none is scheduled.</returns>
    DateTime? GetNextSlotStartUtc(IGameContext gameContext);
}
