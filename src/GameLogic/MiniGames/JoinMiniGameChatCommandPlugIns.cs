// <copyright file="JoinMiniGameChatCommandPlugIns.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlayerActions.MiniGames;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The base class of chat commands, with which a player joins a mini game from anywhere while its entrance is open.
/// The same requirements as for the entrance through the NPC apply, e.g. the ticket, if the mini game requires one.
/// </summary>
public abstract class JoinMiniGameChatCommandPlugInBase : IChatCommandPlugIn
{
    private readonly EnterMiniGameAction _enterAction = new();

    /// <inheritdoc />
    public abstract string Key { get; }

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <summary>
    /// Gets the type of the mini game.
    /// </summary>
    protected abstract MiniGameType MiniGameType { get; }

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (player.Attributes is null || player.SelectedCharacter is null)
        {
            return;
        }

        var level = player.Attributes[Stats.Level];
        var definitions = player.GameContext.Configuration.MiniGameDefinitions
            .Where(d => d.Type == this.MiniGameType && d.IsAvailable())
            .OrderBy(d => d.GameLevel)
            .ToList();

        // Like the entrance itself, special characters (e.g. the Magic Gladiator) use their own level ranges.
        var isSpecialCharacter = player.SelectedCharacter.IsSpecialCharacter();
        var definition = definitions.FirstOrDefault(d => isSpecialCharacter
                             ? level >= d.MinimumSpecialCharacterLevel && level <= d.MaximumSpecialCharacterLevel
                             : level >= d.MinimumCharacterLevel && level <= d.MaximumCharacterLevel)
                         ?? definitions.FirstOrDefault();
        if (definition is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MiniGameEnterFailed)).ConfigureAwait(false);
            return;
        }

        await this._enterAction.TryEnterMiniGameAsync(player, this.MiniGameType, definition.GameLevel, 0xFF).ConfigureAwait(false);
    }
}

/// <summary>
/// The chat command to join the Devil Square from anywhere while its entrance is open.
/// </summary>
[Guid("6F1C2A84-3B5D-4E9A-8C71-2D4E6F8A9B01")]
[PlugIn]
[Display(Name = "Chat command: /joinds", Description = "Joins the Devil Square from anywhere while its entrance is open.")]
[ChatCommandHelp(Command)]
public class JoinDevilSquareChatCommandPlugIn : JoinMiniGameChatCommandPlugInBase
{
    private const string Command = "/joinds";

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    protected override MiniGameType MiniGameType => MiniGameType.DevilSquare;
}

/// <summary>
/// The chat command to join the Blood Castle from anywhere while its entrance is open.
/// </summary>
[Guid("6F1C2A84-3B5D-4E9A-8C71-2D4E6F8A9B02")]
[PlugIn]
[Display(Name = "Chat command: /joinbc", Description = "Joins the Blood Castle from anywhere while its entrance is open.")]
[ChatCommandHelp(Command)]
public class JoinBloodCastleChatCommandPlugIn : JoinMiniGameChatCommandPlugInBase
{
    private const string Command = "/joinbc";

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    protected override MiniGameType MiniGameType => MiniGameType.BloodCastle;
}
