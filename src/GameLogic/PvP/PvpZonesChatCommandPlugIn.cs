// <copyright file="PvpZonesChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PvP;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The chat command which lists the PvP spots of the <see cref="PvpSpotsPlugIn"/>.
/// </summary>
[Guid("2D7E5B1A-4F3C-4E8B-A6D2-9C1F0E3B7A52")]
[PlugIn]
[Display(Name = "Chat command: /zones", Description = "Lists the PvP zones (spots).")]
[ChatCommandHelp(Command)]
public class PvpZonesChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/zones";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (PvpSpotsPlugIn.GetActive(player.GameContext)?.Configuration is not { } configuration)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.PvpNotActive)).ConfigureAwait(false);
            return;
        }

        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.PvpZonesHeader)).ConfigureAwait(false);
        foreach (var spot in configuration.Spots)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.PvpZoneEntry), spot.Name, spot.X1, spot.Y1, spot.X2, spot.Y2).ConfigureAwait(false);
        }
    }
}
