// <copyright file="IDuelWonPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which is called when a duel was finished with a winner, i.e. not cancelled.
/// </summary>
[Guid("0B6E3C1F-8A47-4D29-9F51-3E2D7C6A4B81")]
[PlugInPoint("Duel won", "Plugins which are called when a player won a duel.")]
public interface IDuelWonPlugIn
{
    /// <summary>
    /// Is called when the winner won the duel against the loser.
    /// </summary>
    /// <param name="winner">The winner of the duel.</param>
    /// <param name="loser">The loser of the duel.</param>
    /// <returns>The task.</returns>
    ValueTask DuelWonAsync(Player winner, Player loser);
}
