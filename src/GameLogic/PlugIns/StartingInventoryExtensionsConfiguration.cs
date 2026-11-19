// <copyright file="StartingInventoryExtensionsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// The configuration of the <see cref="StartingInventoryExtensionsPlugIn"/>.
/// </summary>
public class StartingInventoryExtensionsConfiguration
{
    /// <summary>
    /// Gets or sets the number of inventory extensions which every character has unlocked.
    /// It can't exceed the number which the client supports, <see cref="InventoryConstants.MaximumNumberOfExtensions"/>.
    /// </summary>
    public int Extensions { get; set; } = InventoryConstants.MaximumNumberOfExtensions;
}
