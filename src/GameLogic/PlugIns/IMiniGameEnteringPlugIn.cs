// <copyright file="IMiniGameEnteringPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which is called when a player is about to enter a mini game, e.g. the Devil Square or the Blood Castle.
/// </summary>
/// <remarks>
/// It's called after the mini game accepted the player (it's open, not full, and the equipped items are allowed),
/// while the entrance of the mini game is locked. The player enters when no plugin cancels it.
/// It's not called when a player rejoins a game which it entered before it lost its connection.
/// </remarks>
[Guid("F4390008-7822-4A56-8DE3-46766BBD5E30")]
[PlugInPoint("Mini game entering", "Plugins which are called when a player is about to enter a mini game. They can deny the entrance.")]
public interface IMiniGameEnteringPlugIn
{
    /// <summary>
    /// Is called when a player is about to enter a mini game.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="miniGame">The mini game.</param>
    /// <param name="eventArgs">The <see cref="CancelEventArgs"/> instance containing the event data. The entrance can be denied by setting <see cref="CancelEventArgs.Cancel"/> to <c>true</c>.
    /// The plugin should then show the reason to the player.</param>
    ValueTask MiniGameEnteringAsync(Player player, MiniGameContext miniGame, CancelEventArgs eventArgs);
}
