// <copyright file="HuntingPointsChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.HuntingRewards;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The chat command which shows the hunting points and the offers ("/hunt"),
/// and exchanges points for an offer ("/hunt bless").
/// </summary>
[Guid("5A1E7C33-9D2B-4F6A-8E41-7B3C2D1F0A31")]
[PlugIn]
[Display(Name = "Chat command: /hunt", Description = "Shows the hunting points and the offers, and exchanges points with '/hunt <offer>'.")]
[ChatCommandHelp(Command)]
public class HuntingPointsChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/hunt";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (HuntingPointsPlugIn.GetActive(player.GameContext)?.Configuration is not { } configuration
            || player.Attributes is not { } attributes)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.HuntingPointsNotActive)).ConfigureAwait(false);
            return;
        }

        var argument = command.Length > Command.Length ? command[Command.Length..].Trim() : string.Empty;
        if (argument.Length == 0)
        {
            HuntingPointsPlugIn.EnsureCurrentDay(player, DateTime.UtcNow);
            var remaining = configuration.MaximumHuntingTimePerDay - TimeSpan.FromSeconds(attributes[HuntingPointStats.SecondsOfDay]);
            await player.ShowLocalizedBlueMessageAsync(
                    nameof(PlayerMessage.HuntingPointsOverview),
                    (int)attributes[HuntingPointStats.Points],
                    configuration.MinimumLevel,
                    Math.Max(0, (int)remaining.TotalMinutes))
                .ConfigureAwait(false);
            foreach (var offer in configuration.Offers)
            {
                await player.ShowLocalizedBlueMessageAsync(
                        nameof(PlayerMessage.HuntingPointsOffer),
                        offer.Key,
                        RewardItems.GetName(player, offer.Name, offer.ItemGroup, offer.ItemNumber, offer.ItemLevel),
                        offer.Points)
                    .ConfigureAwait(false);
            }

            return;
        }

        if (configuration.Offers.FirstOrDefault(o => string.Equals(o.Key, argument, StringComparison.OrdinalIgnoreCase)) is not { } selected
            || RewardItems.FindDefinition(player, selected.ItemGroup, selected.ItemNumber) is not { } definition)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.HuntingPointsUnknownOffer), argument).ConfigureAwait(false);
            return;
        }

        if (attributes[HuntingPointStats.Points] < selected.Points)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.HuntingPointsNotEnough), selected.Points).ConfigureAwait(false);
            return;
        }

        var item = await RewardItems.TryGiveAsync(player, definition, selected.ItemLevel, () => attributes[HuntingPointStats.Points] -= selected.Points).ConfigureAwait(false);
        if (item is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalsInventoryFull)).ConfigureAwait(false);
            return;
        }

        await player.ShowLocalizedBlueMessageAsync(
                nameof(PlayerMessage.HuntingPointsExchanged),
                RewardItems.GetName(player, selected.Name, selected.ItemGroup, selected.ItemNumber, selected.ItemLevel),
                (int)attributes[HuntingPointStats.Points])
            .ConfigureAwait(false);
        await player.SaveProgressAsync().ConfigureAwait(false);
    }
}
