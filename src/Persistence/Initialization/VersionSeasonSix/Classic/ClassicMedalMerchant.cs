// <copyright file="ClassicMedalMerchant.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;

/// <summary>
/// Adds the medal merchant to the classic server: Thompson the Merchant stands next to the fountain of Lorencia
/// and sells the Box of Kundun +3 and +4 for silver medals, see <see cref="MedalMerchantPlugIn"/>.
/// </summary>
/// <remarks>
/// Thompson the Merchant is an NPC of the original game which the Season 6 data defines, but doesn't place on any map.
/// It has to be applied after the <see cref="ClassicMerchantStores"/>, because they add potions to all stores.
/// </remarks>
public static class ClassicMedalMerchant
{
    /// <summary>
    /// The x coordinate of the merchant in Lorencia, next to the fountain.
    /// </summary>
    public const byte X = 136;

    /// <summary>
    /// The y coordinate of the merchant in Lorencia, next to the fountain.
    /// </summary>
    public const byte Y = 131;

    /// <summary>
    /// The number of the monster spawn of the merchant in Lorencia, after the spawns of the Lorencia NPCs.
    /// </summary>
    private const short SpawnNumber = 28;

    private const short LorenciaNumber = 0;

    /// <summary>
    /// Applies the merchant. It can be applied multiple times with the same result.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The task.</returns>
    public static async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.Monsters.FirstOrDefault(m => m.Number == MedalMerchantConfiguration.DefaultMerchantNumber) is not { } merchant
            || gameConfiguration.Maps.FirstOrDefault(m => m.Number == LorenciaNumber) is not { } lorencia)
        {
            return;
        }

        merchant.IsActive = true;
        merchant.ObjectKind = NpcObjectKind.PassiveNpc;
        merchant.NpcWindow = NpcWindow.Merchant;
        if (merchant.MerchantStore is null)
        {
            merchant.MerchantStore = context.CreateNew<ItemStorage>();
            merchant.MerchantStore.SetGuid(merchant.Number);
        }

        await ApplyStoreAsync(context, gameConfiguration, merchant.MerchantStore).ConfigureAwait(false);
        ApplySpawn(context, lorencia, merchant);
    }

    /// <summary>
    /// Gets the identifier of the monster spawn of the merchant.
    /// </summary>
    /// <returns>The identifier.</returns>
    internal static Guid GetSpawnId() => GuidHelper.CreateGuid<MonsterSpawnArea>(LorenciaNumber, SpawnNumber);

    private static async ValueTask ApplyStoreAsync(IContext context, GameConfiguration gameConfiguration, ItemStorage store)
    {
        var prices = MedalMerchantConfiguration.CreateDefault().Prices;
        bool HasPrice(Item item) => item.Definition is { } d && prices.Any(p => p.ItemGroup == d.Group && p.ItemNumber == d.Number && p.ItemLevel == item.Level);

        foreach (var item in store.Items.Where(i => !HasPrice(i)).ToList())
        {
            store.Items.Remove(item);
            await context.DeleteAsync(item).ConfigureAwait(false);
        }

        foreach (var price in prices)
        {
            if (gameConfiguration.Items.FirstOrDefault(i => i.Group == price.ItemGroup && i.Number == price.ItemNumber) is not { } definition
                || store.Items.Any(i => i.Definition == definition && i.Level == price.ItemLevel)
                || ClassicMerchantStores.FindFreeSlot(store.Items, definition) is not { } slot)
            {
                continue;
            }

            var item = context.CreateNew<Item>();
            item.Definition = definition;
            item.Level = price.ItemLevel;
            item.Durability = 1;
            item.ItemSlot = slot;
            store.Items.Add(item);
        }
    }

    private static void ApplySpawn(IContext context, GameMapDefinition lorencia, MonsterDefinition merchant)
    {
        var id = GetSpawnId();
        var spawn = lorencia.MonsterSpawns.FirstOrDefault(s => s.GetId() == id);
        if (spawn is null)
        {
            spawn = context.CreateNew<MonsterSpawnArea>();
            spawn.SetGuid(id);
            lorencia.MonsterSpawns.Add(spawn);
        }

        spawn.GameMap = lorencia;
        spawn.MonsterDefinition = merchant;
        spawn.Quantity = 1;
        spawn.Direction = Direction.South;
        spawn.SpawnTrigger = SpawnTrigger.Automatic;
        spawn.X1 = X;
        spawn.X2 = X;
        spawn.Y1 = Y;
        spawn.Y2 = Y;
    }
}
