// <copyright file="MedalsChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Medals;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The chat command which shows the medals and the exchange offers ("/medals"),
/// and exchanges medals for an offer ("/medals bless").
/// </summary>
[Guid("3E9D4B27-6C1A-4D85-B0F3-8A2E5C7D1B11")]
[PlugIn]
[Display(Name = "Chat command: /medals", Description = "Shows the medals and the exchange offers, and exchanges medals with '/medals <offer>'.")]
[ChatCommandHelp(Command)]
public class MedalsChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/medals";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (MiniGameMedalsPlugIn.GetActive(player.GameContext)?.Configuration is not { } configuration
            || player.Attributes is not { } attributes)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalsNotActive)).ConfigureAwait(false);
            return;
        }

        var argument = command.Length > Command.Length ? command[Command.Length..].Trim() : string.Empty;
        if (argument.Length == 0)
        {
            await ShowOverviewAsync(player, configuration).ConfigureAwait(false);
            return;
        }

        if (configuration.Offers.FirstOrDefault(o => string.Equals(o.Key, argument, StringComparison.OrdinalIgnoreCase)) is not { } offer)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalsUnknownOffer), argument).ConfigureAwait(false);
            return;
        }

        if (attributes[MedalStats.GoldMedals] < offer.GoldMedals || attributes[MedalStats.SilverMedals] < offer.SilverMedals)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalsNotEnough), offer.GoldMedals, offer.SilverMedals).ConfigureAwait(false);
            return;
        }

        if (RewardItems.FindDefinition(player, offer.ItemGroup, offer.ItemNumber) is not { } definition)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalsUnknownOffer), argument).ConfigureAwait(false);
            return;
        }

        var item = await RewardItems.TryGiveAsync(player, definition, offer.ItemLevel, () =>
        {
            attributes[MedalStats.GoldMedals] -= offer.GoldMedals;
            attributes[MedalStats.SilverMedals] -= offer.SilverMedals;
        }).ConfigureAwait(false);
        if (item is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalsInventoryFull)).ConfigureAwait(false);
            return;
        }

        await player.ShowLocalizedBlueMessageAsync(
                nameof(PlayerMessage.MedalsExchanged),
                RewardItems.GetName(player, offer.Name, offer.ItemGroup, offer.ItemNumber, offer.ItemLevel),
                (int)attributes[MedalStats.GoldMedals],
                (int)attributes[MedalStats.SilverMedals])
            .ConfigureAwait(false);
        await player.SaveProgressAsync().ConfigureAwait(false);
    }

    private static async ValueTask ShowOverviewAsync(Player player, MiniGameMedalsConfiguration configuration)
    {
        var attributes = player.Attributes!;
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalsOverview), (int)attributes[MedalStats.GoldMedals], (int)attributes[MedalStats.SilverMedals]).ConfigureAwait(false);
        foreach (var offer in configuration.Offers)
        {
            var itemName = RewardItems.GetName(player, offer.Name, offer.ItemGroup, offer.ItemNumber, offer.ItemLevel);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalsOffer), offer.Key, itemName, offer.GoldMedals, offer.SilverMedals).ConfigureAwait(false);
        }
    }
}
