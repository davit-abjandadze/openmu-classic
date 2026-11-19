// <copyright file="ICharacterEnteringWorldPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which is called when a selected character is about to enter the world,
/// before its storages, attributes and skills are built.
/// </summary>
/// <remarks>
/// It allows to adapt the persisted data of the character (e.g. the number of inventory extensions),
/// so that the changes already apply to the storages which are built for this character and are sent to the client.
/// </remarks>
[Guid("3E8D5B47-0C2A-4F19-9B6E-71A4D2C8F503")]
[PlugInPoint("Character entering world", "Is called when a selected character is about to enter the world, before its storages are built.")]
public interface ICharacterEnteringWorldPlugIn
{
    /// <summary>
    /// Is called when the selected character of the player is about to enter the world.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="character">The selected character.</param>
    void CharacterEnteringWorld(Player player, Character character);
}
