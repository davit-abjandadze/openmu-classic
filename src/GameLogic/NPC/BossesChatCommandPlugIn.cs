// <copyright file="BossesChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The chat command which shows all bosses of all maps ("/bosses"): if a boss is alive, or the remaining
/// time until a killed boss respawns. The bosses are the ones of the <see cref="BossMapMarkersPlugIn"/>.
/// </summary>
[Guid("7C2E4A91-5B3D-4F68-9E1A-D04B8C6F2A73")]
[PlugIn]
[Display(Name = "Chat command: /bosses", Description = "Shows all bosses, if they are alive, or the remaining time until they respawn.")]
[ChatCommandHelp(Command)]
public class BossesChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/bosses";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (BossMapMarkersPlugIn.GetActive(player.GameContext) is not { } bossPlugIn)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.BossesNotActive)).ConfigureAwait(false);
            return;
        }

        var statuses = await bossPlugIn.GetBossStatusesAsync(player.GameContext).ConfigureAwait(false);
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.BossesOverview)).ConfigureAwait(false);
        foreach (var status in statuses)
        {
            if (status.IsAlive)
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.BossAlive), status.Name, status.MapName).ConfigureAwait(false);
            }
            else
            {
                var remaining = status.RespawnIn;
                await player.ShowLocalizedBlueMessageAsync(
                        nameof(PlayerMessage.BossRespawnsIn),
                        status.Name,
                        status.MapName,
                        (int)remaining.TotalHours,
                        remaining.Minutes,
                        remaining.Seconds)
                    .ConfigureAwait(false);
            }
        }
    }
}
