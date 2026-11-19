// <copyright file="StartingInventoryExtensionsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Unlocks the configured number of inventory extensions for every character from the start:
/// when the character is created, and when an existing character with fewer extensions enters the world.
/// It never takes extensions away.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.StartingInventoryExtensionsPlugIn_Name), Description = nameof(PlugInResources.StartingInventoryExtensionsPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("A7F2C6D1-4B8E-4E3A-9D05-6C1B7E2F8A94")]
public class StartingInventoryExtensionsPlugIn : ICharacterCreatedPlugIn, ICharacterEnteringWorldPlugIn, ISupportCustomConfiguration<StartingInventoryExtensionsConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public StartingInventoryExtensionsConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public void CharacterCreated(Account account, Character createdCharacter, IContext persistenceContext, GameConfiguration gameConfiguration, ILogger logger)
    {
        this.Unlock(createdCharacter);
    }

    /// <inheritdoc />
    public void CharacterEnteringWorld(Player player, Character character)
    {
        this.Unlock(character);
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new StartingInventoryExtensionsConfiguration();

    private void Unlock(Character character)
    {
        var configuration = this.Configuration ?? new StartingInventoryExtensionsConfiguration();
        var extensions = Math.Clamp(configuration.Extensions, 0, InventoryConstants.MaximumNumberOfExtensions);
        if (character.InventoryExtensions < extensions)
        {
            character.InventoryExtensions = extensions;
        }
    }
}
