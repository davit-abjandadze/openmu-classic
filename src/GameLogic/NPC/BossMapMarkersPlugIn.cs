// <copyright file="BossMapMarkersPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.World;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Shows the bosses of the current map on the map of the client (Tab), with the remaining time until
/// a killed boss respawns. The bosses are sent when the player enters a map, and to all players of
/// the map when a boss got killed and when it respawned.
/// </summary>
[Guid("3F0C7A9E-6B21-4D5E-A8C4-92E1B7D06F35")]
[PlugIn]
[Display(Name = "Boss markers on the map", Description = "Shows the bosses of the current map on the map of the open source client, with the remaining time until a killed boss respawns.")]
public class BossMapMarkersPlugIn : IObjectAddedToMapPlugIn, IAttackableGotKilledPlugIn, ISupportCustomConfiguration<BossMapMarkersConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// The additional delay after the respawn time, after which the respawned boss is sent, so that it's alive by then.
    /// </summary>
    private static readonly TimeSpan RespawnNotificationDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The killed bosses which are waiting for their respawn, with the time of the respawn.
    /// </summary>
    private readonly ConcurrentDictionary<Monster, DateTime> _respawnTimes = new();

    /// <inheritdoc />
    public BossMapMarkersConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new BossMapMarkersConfiguration();

    /// <summary>
    /// Gets the active instance of this plugin.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The active instance, or <see langword="null"/> if it's not active.</returns>
    public static BossMapMarkersPlugIn? GetActive(IGameContext gameContext)
        => gameContext.PlugInManager.GetActivePlugInsOf<IAttackableGotKilledPlugIn>().OfType<BossMapMarkersPlugIn>().FirstOrDefault();

    /// <inheritdoc />
    public async ValueTask ObjectAddedToMapAsync(GameMap map, ILocateable addedObject)
    {
        if (addedObject is Player player)
        {
            await ShowAsync(player, map, this.GetMarkers(map)).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killed is not Monster monster || !this.IsBoss(monster) || monster.CurrentMap is not { } map)
        {
            return;
        }

        var respawnDelay = monster.Definition.RespawnDelay;
        this._respawnTimes[monster] = DateTime.UtcNow + respawnDelay;
        await this.ShowToPlayersOfMapAsync(map).ConfigureAwait(false);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(respawnDelay + RespawnNotificationDelay).ConfigureAwait(false);
                await this.ShowToPlayersOfMapAsync(map).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.Fail($"Unexpected error when showing the respawned boss {monster}: {ex}", ex.StackTrace);
            }
        });
    }

    /// <summary>
    /// Gets the bosses of a map.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <returns>The alive bosses at their position, and the killed bosses at the place of their death.</returns>
    internal IReadOnlyCollection<BossMapMarker> GetMarkers(GameMap map)
    {
        var markers = new List<BossMapMarker>();
        foreach (var npc in map.GetNpcsInRange(new Point(128, 128), byte.MaxValue))
        {
            if (npc is Monster { IsAlive: true } monster && this.IsBoss(monster))
            {
                markers.Add(new BossMapMarker(GetName(monster), monster.Position, true, TimeSpan.Zero));
            }
        }

        var now = DateTime.UtcNow;
        foreach (var (monster, respawnTime) in this._respawnTimes)
        {
            if (monster.IsDisposed || monster.IsAlive)
            {
                // It respawned or got removed, so it doesn't wait anymore.
                this._respawnTimes.TryRemove(monster, out _);
                continue;
            }

            if (monster.CurrentMap == map)
            {
                var remaining = respawnTime > now ? respawnTime - now : TimeSpan.Zero;
                markers.Add(new BossMapMarker(GetName(monster), monster.Position, false, remaining));
            }
        }

        return markers;
    }

    /// <summary>
    /// Gets the states of all bosses of all maps, ordered by the map number.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The states of the bosses.</returns>
    /// <remarks>
    /// A map which wasn't created yet has no spawned monsters. Its bosses spawn as soon as the map gets
    /// created by the first player entering it, so they are reported as alive.
    /// </remarks>
    internal async ValueTask<IReadOnlyList<BossStatus>> GetBossStatusesAsync(IGameContext gameContext)
    {
        var configuration = this.Configuration ??= new BossMapMarkersConfiguration();
        var statuses = new List<BossStatus>();
        foreach (var mapDefinition in gameContext.Configuration.Maps.OrderBy(m => m.Number))
        {
            var bossSpawns = mapDefinition.MonsterSpawns
                .Where(s => s.SpawnTrigger == SpawnTrigger.Automatic
                            && s.MonsterDefinition is { } definition
                            && configuration.BossMonsterNumbers.Contains(definition.Number))
                .ToList();
            if (bossSpawns.Count == 0)
            {
                continue;
            }

            var mapName = mapDefinition.Name.ValueInNeutralLanguage;
            if (await gameContext.GetMapAsync((ushort)mapDefinition.Number, false).ConfigureAwait(false) is { } map)
            {
                statuses.AddRange(this.GetMarkers(map).Select(m => new BossStatus(m.Name, mapName, m.IsAlive, m.RespawnIn)));
                continue;
            }

            foreach (var spawn in bossSpawns)
            {
                var name = spawn.MonsterDefinition!.Designation.ValueInNeutralLanguage;
                statuses.AddRange(Enumerable.Repeat(new BossStatus(name, mapName, true, TimeSpan.Zero), spawn.Quantity));
            }
        }

        return statuses;
    }

    private static string GetName(Monster monster) => monster.Definition.Designation.ValueInNeutralLanguage;

    private static async ValueTask ShowAsync(Player player, GameMap map, IReadOnlyCollection<BossMapMarker> markers)
    {
        await player.InvokeViewPlugInAsync<IShowBossMapMarkersPlugIn>(p => p.ShowBossMapMarkersAsync(map.Definition.Number, markers)).ConfigureAwait(false);
    }

    private async ValueTask ShowToPlayersOfMapAsync(GameMap map)
    {
        var markers = this.GetMarkers(map);
        foreach (var player in map.GetPlayers())
        {
            await ShowAsync(player, map, markers).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Determines whether the monster is a boss which is shown on the map: a configured boss which
    /// respawns by itself, so not one of an event or a summon.
    /// </summary>
    private bool IsBoss(Monster monster)
    {
        var configuration = this.Configuration ??= new BossMapMarkersConfiguration();
        return monster.SummonedBy is null
               && monster.SpawnArea.SpawnTrigger == SpawnTrigger.Automatic
               && configuration.BossMonsterNumbers.Contains(monster.Definition.Number);
    }
}

/// <summary>
/// The state of a boss.
/// </summary>
/// <param name="Name">The name of the boss.</param>
/// <param name="MapName">The name of the map of the boss.</param>
/// <param name="IsAlive">If set to <c>true</c>, the boss is alive.</param>
/// <param name="RespawnIn">The remaining time until a killed boss respawns.</param>
public sealed record BossStatus(string Name, string MapName, bool IsAlive, TimeSpan RespawnIn);
