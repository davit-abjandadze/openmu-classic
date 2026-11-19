// <copyright file="ItemExchangeChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemExchange;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The chat command which exchanges a number of excellent items for a chosen item:
/// "/exchange dragon armor" shows which items would be exchanged, "/exchange dragon armor yes" exchanges them.
/// The new item is +0 with one excellent option (with a chance of two or three), a chance of luck and a random additional option.
/// Equipped items are never exchanged.
/// </summary>
[Guid("9A2D6E15-3C4B-4F7A-8D61-5B0E2C9F7A01")]
[PlugIn]
[Display(Name = "Chat command: /exchange", Description = "Exchanges excellent items for a chosen set piece or weapon: '/exchange dragon armor' shows the items, '/exchange dragon armor yes' exchanges them.")]
[ChatCommandHelp(Command)]
public class ItemExchangeChatCommandPlugIn : IChatCommandPlugIn, ISupportCustomConfiguration<ItemExchangeConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    private const string Command = "/exchange";
    private const string ConfirmWord = "yes";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public ItemExchangeConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ItemExchangeConfiguration();

    /// <summary>
    /// Selects the items of the inventory which would be exchanged: not equipped excellent items with the minimum drop level, the cheapest first.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The selected items. If there are not enough, less than <see cref="ItemExchangeConfiguration.RequiredItems"/>.</returns>
    internal static IReadOnlyList<Item> SelectItems(Player player, ItemExchangeConfiguration configuration)
    {
        return (player.Inventory?.Items ?? [])
            .Where(item => item.ItemSlot > InventoryConstants.LastEquippableItemSlotIndex
                           && item.ItemSlot < InventoryConstants.FirstStoreItemSlotIndex
                           && item.Definition is { } definition
                           && definition.DropLevel >= configuration.MinimumDropLevel
                           && item.IsExcellent())
            .OrderBy(item => item.Definition!.DropLevel)
            .ThenBy(item => item.ItemOptions.Count)
            .Take(configuration.RequiredItems)
            .ToList();
    }

    /// <summary>
    /// Adds the random options to the new item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="createOptionLink">The function which creates a new option link.</param>
    internal static void AddRandomOptions(Item item, ItemExchangeConfiguration configuration, Func<ItemOptionLink> createOptionLink)
    {
        var definition = item.Definition!;
        var excellentOptions = definition.PossibleItemOptions
            .SelectMany(o => o.PossibleOptions)
            .Where(o => object.Equals(o.OptionType, ItemOptionTypes.Excellent))
            .ToList();
        var optionCount = Rand.NextRandomBool(configuration.ThreeOptionsChance) ? 3
            : Rand.NextRandomBool(configuration.TwoOptionsChance) ? 2
            : 1;
        foreach (var excellentOption in excellentOptions.OrderBy(_ => Rand.NextInt(0, 1000)).Take(optionCount))
        {
            var link = createOptionLink();
            link.ItemOption = excellentOption;
            item.ItemOptions.Add(link);
        }

        if (Rand.NextRandomBool(configuration.LuckChance)
            && definition.PossibleItemOptions.SelectMany(o => o.PossibleOptions).FirstOrDefault(o => object.Equals(o.OptionType, ItemOptionTypes.Luck)) is { } luck)
        {
            var link = createOptionLink();
            link.ItemOption = luck;
            item.ItemOptions.Add(link);
        }

        var optionLevel = Rand.NextInt(0, Math.Max(0, configuration.MaximumOptionLevel) + 1);
        if (optionLevel > 0
            && definition.PossibleItemOptions.SelectMany(o => o.PossibleOptions).FirstOrDefault(o => object.Equals(o.OptionType, ItemOptionTypes.Option)) is { } option)
        {
            var link = createOptionLink();
            link.ItemOption = option;
            link.Level = optionLevel;
            item.ItemOptions.Add(link);
        }
    }

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var configuration = this.Configuration ??= new ItemExchangeConfiguration();
        var argument = command.Length > Command.Length ? command[Command.Length..].Trim() : string.Empty;
        var confirmed = argument.EndsWith(" " + ConfirmWord, StringComparison.OrdinalIgnoreCase);
        if (confirmed)
        {
            argument = argument[..^(ConfirmWord.Length + 1)].Trim();
        }

        if (argument.Length == 0)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ItemExchangeUsage), configuration.RequiredItems, configuration.MinimumDropLevel).ConfigureAwait(false);
            await player.ShowBlueMessageAsync(string.Join(", ", configuration.TargetItems)).ConfigureAwait(false);
            return;
        }

        var targetName = configuration.TargetItems.FirstOrDefault(name => string.Equals(name, argument, StringComparison.OrdinalIgnoreCase));
        var target = targetName is null
            ? null
            : player.GameContext.Configuration.Items.FirstOrDefault(i => i.IsActive && string.Equals(i.Name.ValueInNeutralLanguage, targetName, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ItemExchangeUnknownTarget), argument).ConfigureAwait(false);
            return;
        }

        var items = SelectItems(player, configuration);
        if (items.Count < configuration.RequiredItems)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ItemExchangeNotEnoughItems), configuration.RequiredItems, configuration.MinimumDropLevel, items.Count).ConfigureAwait(false);
            return;
        }

        var itemNames = string.Join(", ", items.Select(i => i.Definition!.Name.GetTranslation(player.Culture)));
        var targetDisplayName = target.Name.GetTranslation(player.Culture);
        if (!confirmed)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ItemExchangePreview), targetDisplayName, itemNames).ConfigureAwait(false);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ItemExchangeConfirmHint), argument).ConfigureAwait(false);
            return;
        }

        foreach (var item in items)
        {
            await player.DestroyInventoryItemAsync(item).ConfigureAwait(false);
        }

        var newItem = await RewardItems.TryGiveAsync(
                player,
                target,
                0,
                configure: item => AddRandomOptions(item, configuration, () => player.PersistenceContext.CreateNew<ItemOptionLink>()))
            .ConfigureAwait(false);
        if (newItem is null)
        {
            // Can't happen, because the exchanged items freed enough space for at least the same size, but just in case.
            player.Logger.LogError("The exchanged item {0} couldn't be added to the inventory of {1}.", target, player);
            return;
        }

        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ItemExchangeDone), targetDisplayName).ConfigureAwait(false);
        await player.SaveProgressAsync().ConfigureAwait(false);
    }
}
