// <copyright file="HuntingJewelChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.HuntingRewards;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The chat command which shows the progress of the <see cref="HuntingJewelPlugIn"/>.
/// </summary>
[Guid("5A1E7C33-9D2B-4F6A-8E41-7B3C2D1F0A11")]
[PlugIn]
[Display(Name = "Chat command: /jewels", Description = "Shows the hunting time until the next jewel and the jewels of the day.")]
[ChatCommandHelp(Command)]
public class HuntingJewelChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/jewels";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (HuntingJewelPlugIn.GetActive(player.GameContext)?.Configuration is not { } configuration
            || player.Attributes is not { } attributes)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.HuntingJewelNotActive)).ConfigureAwait(false);
            return;
        }

        HuntingJewelPlugIn.EnsureCurrentDay(player, DateTime.UtcNow);
        var jewels = (int)attributes[HuntingJewelStats.JewelsOfDay];
        if (jewels >= configuration.MaximumJewelsPerDay)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.HuntingJewelDailyLimitReached), configuration.MaximumJewelsPerDay).ConfigureAwait(false);
            return;
        }

        var remaining = configuration.HuntingTimePerJewel - TimeSpan.FromSeconds(attributes[HuntingJewelStats.HuntingSeconds]);
        await player.ShowLocalizedBlueMessageAsync(
                nameof(PlayerMessage.HuntingJewelProgress),
                Math.Max(0, (int)Math.Ceiling(remaining.TotalMinutes)),
                jewels,
                configuration.MaximumJewelsPerDay)
            .ConfigureAwait(false);
    }
}
