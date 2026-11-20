// <copyright file="MerchantStoreItemsArgs.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// The items of a merchant store, see <see cref="IMerchantStoreItemsPlugIn"/>.
/// </summary>
public class MerchantStoreItemsArgs
{
    /// <summary>
    /// Gets or sets the items which the merchant offers instead of the items of its configured merchant store.
    /// When it's <c>null</c>, the items of the configured merchant store are offered.
    /// The items must have distinct slots of the store; they can be temporary items.
    /// </summary>
    public ICollection<Item>? Items { get; set; }
}
