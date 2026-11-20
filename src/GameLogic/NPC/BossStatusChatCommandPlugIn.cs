// <copyright file="BossStatusChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The chat command which shows which bosses are alive and where.
/// </summary>
[Guid("7C4B1E92-5A3D-4F8B-9E26-1D0C3A7B5F01")]
[PlugIn]
[Display(Name = "Chat command: /bosses", Description = "Shows which bosses are alive and where.")]
[ChatCommandHelp(Command)]
public class BossStatusChatCommandPlugIn : IChatCommandPlugIn, ISupportCustomConfiguration<BossStatusChatCommandPlugIn.BossStatusConfiguration>, ISupportDefaultCustomConfiguration
{
    private const string Command = "/bosses";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public BossStatusConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new BossStatusConfiguration();

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var configuration = this.Configuration ??= new BossStatusConfiguration();
        var bossNumbers = configuration.BossMonsterNumbers.ToHashSet();
        var aliveBosses = new List<(string Name, string Map, Point Position)>();
        foreach (var map in await player.GameContext.GetMapsAsync().ConfigureAwait(false))
        {
            foreach (var npc in map.GetNpcsInRange(new Point(128, 128), 255))
            {
                if (npc is Monster { IsAlive: true, SummonedBy: null } monster
                    && bossNumbers.Contains(monster.Definition.Number))
                {
                    aliveBosses.Add((
                        monster.Definition.Designation.GetTranslation(player.Culture) ?? string.Empty,
                        map.Definition.Name.GetTranslation(player.Culture) ?? string.Empty,
                        monster.Position));
                }
            }
        }

        if (aliveBosses.Count == 0)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.BossesNoneAlive)).ConfigureAwait(false);
            return;
        }

        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.BossesHeader), aliveBosses.Count).ConfigureAwait(false);
        foreach (var boss in aliveBosses.OrderBy(b => b.Map).ThenBy(b => b.Name))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.BossesEntry), boss.Name, boss.Map, boss.Position.X, boss.Position.Y).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The configuration of the <see cref="BossStatusChatCommandPlugIn"/>.
    /// </summary>
    public class BossStatusConfiguration
    {
        /// <summary>
        /// Gets or sets the numbers of the monster definitions which are bosses.
        /// </summary>
        [Display(Name = "Boss monster numbers", Description = "The numbers of the monsters which are listed as bosses.")]
        public IList<short> BossMonsterNumbers { get; set; } =
        [
            38, // Balrog
            49, // Hydra
            59, // Zaikan
            63, // Death Beam Knight
            77, // Dark Phoenix
            309, // Hell Maine
        ];
    }
}
