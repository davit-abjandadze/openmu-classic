// <copyright file="IMerchantStoreItemsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Plugins which can provide the items of a merchant NPC instead of the items of its configured merchant store,
/// e.g. a merchant whose items come from the configuration of a plugin and can be changed while the server is running.
/// </summary>
[Guid("5B0E2C71-94D3-4F6A-8E15-C3A7D2B90F48")]
[PlugInPoint("Merchant store items", "Plugins which can provide the items of a merchant NPC instead of the items of its configured merchant store.")]
public interface IMerchantStoreItemsPlugIn
{
    /// <summary>
    /// Is called when the items of a merchant NPC are shown to a player or bought by a player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="merchant">The merchant NPC.</param>
    /// <param name="eventArgs">The event args. A plugin can set the items of the store.</param>
    void ProvideStoreItems(Player player, NonPlayerCharacter merchant, MerchantStoreItemsArgs eventArgs);
}
