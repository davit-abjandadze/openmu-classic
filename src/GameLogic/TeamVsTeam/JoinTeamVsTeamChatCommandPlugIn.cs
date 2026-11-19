// <copyright file="JoinTeamVsTeamChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.TeamVsTeam;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The chat command to register for the Team vs Team event while its registration is open.
/// </summary>
[Guid("D4C7A1E3-2B5F-4A86-9C30-6E8F1B2D7A11")]
[PlugIn]
[Display(Name = "Chat command: /jointvt", Description = "Registers for the Team vs Team event while its registration is open.")]
[ChatCommandHelp(Command)]
public class JoinTeamVsTeamChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/jointvt";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (TeamVsTeamPlugIn.GetActive(player.GameContext) is not { } plugIn)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.TeamVsTeamNotOpen)).ConfigureAwait(false);
            return;
        }

        await plugIn.TryRegisterAsync(player).ConfigureAwait(false);
    }
}
