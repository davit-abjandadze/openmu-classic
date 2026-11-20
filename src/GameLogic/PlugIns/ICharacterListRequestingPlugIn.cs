// <copyright file="ICharacterListRequestingPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which is called before the character list is shown to a <see cref="Player"/>.
/// </summary>
[Guid("5E7A3C19-2B84-4D6F-A1C3-8F9E0B4D2A71")]
[PlugInPoint("Character list requesting", "Plugins which will be executed before the character list is shown to a player, e.g. to unlock character classes of the account.")]
public interface ICharacterListRequestingPlugIn
{
    /// <summary>
    /// This method is called before the character list is shown to the <see cref="Player"/>.
    /// </summary>
    /// <param name="player">The player whose account is selecting a character.</param>
    void CharacterListRequesting(Player player);
}
