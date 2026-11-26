// <copyright file="ClassicBosses.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;

/// <summary>
/// Adapts the field bosses of the classic server. Each boss respawns after a fixed delay and always drops
/// a Box of Kundun and silver medals, additionally to its normal drops:
/// <list type="bullet">
///   <item>Hydra (Atlans): Box of Kundun +3, 1 silver medal, respawn after 70 minutes.</item>
///   <item>Balrog (Lost Tower 7): Box of Kundun +3, 1 silver medal, respawn after 70 minutes.</item>
///   <item>Zaikan and Death Beam Knight (Tarkan): Box of Kundun +4, 1 silver medal, respawn after 120 minutes.</item>
///   <item>Dark Phoenix (Icarus): Box of Kundun +5, 3 silver medals, respawn after 240 minutes.</item>
///   <item>Hell Maine (Arena): Box of Kundun +5, 3 silver medals, respawn after 240 minutes.</item>
/// </list>
/// Hell Maine originally lives in Aida, which the classic server doesn't offer. It's activated and spawned
/// once in the open eastern part of the Arena, see <see cref="Boss.Spawn"/>.
/// The bosses are also much stronger than in the original data, so that they can't be killed as fast as
/// normal monsters by characters with resets, see <see cref="BossStrength"/>. Hydra and Balrog need about
/// a few minutes for a single character with some resets, the Dark Phoenix a party. Hell Maine is 40% stronger than the Dark Phoenix.
/// The drops are drop item groups of the boss map which are restricted to the boss, so that the same monster
/// doesn't drop them somewhere else, e.g. the Hydra in the Devil Square.
/// </summary>
/// <remarks>
/// The silver medal is the Box of Luck +5, see <see cref="MedalItems"/>. It pays at the medal merchant like the silver medals of the character.
/// </remarks>
public static class ClassicBosses
{
    /// <summary>
    /// The number of normal (chance based) drops of a boss, additionally to its guaranteed drops.
    /// </summary>
    public const int NormalDrops = 2;

    /// <summary>
    /// The first number of the identifiers of the boss drop groups, so that they don't collide with other drop groups.
    /// </summary>
    private const short DropGroupIdPrefix = 0x0B05;

    /// <summary>
    /// The first number of the spawns which are added for bosses, so that they don't collide with the original spawns
    /// and the spawns of <see cref="ClassicFarmSpots"/> (from 0x0F00). The monster number of the boss is added to it.
    /// </summary>
    private const short FirstSpawnNumber = 0x0C00;

    private const byte BoxOfLuckGroup = 14;

    private const short BoxOfLuckNumber = 11;

    /// <summary>
    /// Gets the bosses.
    /// </summary>
    public static IReadOnlyList<Boss> Bosses { get; } =
    [
        new(49, "Hydra", 7, 3, 1, TimeSpan.FromMinutes(70), new(800_000, 1_100, 1_300, 550, 1_800, 450)),
        new(38, "Balrog", 4, 3, 1, TimeSpan.FromMinutes(70), new(800_000, 1_200, 1_400, 550, 1_800, 450)),
        new(59, "Zaikan", 8, 4, 1, TimeSpan.FromMinutes(120), new(2_000_000, 1_800, 2_100, 800, 2_600, 650)),
        new(63, "Death Beam Knight", 8, 4, 1, TimeSpan.FromMinutes(120), new(2_000_000, 1_900, 2_200, 850, 2_700, 700)),
        new(77, "Dark Phoenix", 10, 5, 3, TimeSpan.FromMinutes(240), new(5_000_000, 3_000, 3_500, 1_100, 3_500, 950)),
        new(309, "Hell Maine", 6, 5, 3, TimeSpan.FromMinutes(240), new(7_000_000, 4_200, 4_900, 1_540, 4_900, 1_330), new(90, 90)),
    ];

    /// <summary>
    /// Applies the <see cref="BossStrength"/> of the bosses to their monster definitions.
    /// It can be applied multiple times with the same result.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public static void ApplyStrength(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var boss in Bosses)
        {
            if (gameConfiguration.Monsters.FirstOrDefault(m => m.Number == boss.MonsterNumber) is not { } monster)
            {
                continue;
            }

            ApplyStrength(context, gameConfiguration, monster, boss.Strength);
        }
    }

    /// <summary>
    /// Sets the stats of the <paramref name="strength"/> to the monster definition, without duplicating attributes.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="monster">The monster definition.</param>
    /// <param name="strength">The strength.</param>
    internal static void ApplyStrength(IContext context, GameConfiguration gameConfiguration, MonsterDefinition monster, BossStrength strength)
    {
        SetAttribute(context, gameConfiguration, monster, Stats.MaximumHealth, strength.Health);
        SetAttribute(context, gameConfiguration, monster, Stats.MinimumPhysBaseDmg, strength.MinimumDamage);
        SetAttribute(context, gameConfiguration, monster, Stats.MaximumPhysBaseDmg, strength.MaximumDamage);
        SetAttribute(context, gameConfiguration, monster, Stats.DefenseBase, strength.Defense);
        SetAttribute(context, gameConfiguration, monster, Stats.AttackRatePvm, strength.AttackRate);
        SetAttribute(context, gameConfiguration, monster, Stats.DefenseRatePvm, strength.DefenseRate);
    }

    /// <summary>
    /// Applies the boss changes. It can be applied multiple times with the same result.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public static void Apply(IContext context, GameConfiguration gameConfiguration)
    {
        ApplyStrength(context, gameConfiguration);
        if (ClassicBoxes.GetBoxOfLuck(gameConfiguration) is not { } boxOfLuck)
        {
            return;
        }

        foreach (var boss in Bosses)
        {
            if (gameConfiguration.Monsters.FirstOrDefault(m => m.Number == boss.MonsterNumber) is not { } monster
                || gameConfiguration.Maps.FirstOrDefault(m => m.Number == boss.MapNumber) is not { } map)
            {
                continue;
            }

            monster.RespawnDelay = boss.RespawnDelay;
            if (boss.Spawn is { } position)
            {
                monster.IsActive = true;
                ApplySpawn(context, map, monster, GetSpawnId(boss), position);
            }

            var boxLevel = (byte)(ClassicBoxes.FirstBoxOfKundunLevel + boss.BoxOfKundunPlus - 1);
            var drops = new List<(string Description, byte Level)> { ($"{boss.Name}: Box of Kundun +{boss.BoxOfKundunPlus}", boxLevel) };
            for (var i = 1; i <= boss.SilverMedals; i++)
            {
                drops.Add(($"{boss.Name}: Silver Medal ({i}/{boss.SilverMedals})", MedalItems.SilverMedalLevel));
            }

            for (byte index = 0; index < drops.Count; index++)
            {
                ApplyDropGroup(context, gameConfiguration, map, monster, boxOfLuck, GetDropGroupId(boss, index), drops[index].Description, drops[index].Level);
            }

            monster.NumberOfMaximumItemDrops = Math.Max(monster.NumberOfMaximumItemDrops, drops.Count + NormalDrops);
        }
    }

    /// <summary>
    /// Gets the identifier of a guaranteed drop group of a boss.
    /// </summary>
    /// <param name="boss">The boss.</param>
    /// <param name="index">The index of the drop: 0 is the Box of Kundun, the following are silver medals.</param>
    /// <returns>The identifier.</returns>
    internal static Guid GetDropGroupId(Boss boss, byte index)
        => GuidHelper.CreateGuid<DropItemGroup>(DropGroupIdPrefix, boss.MonsterNumber, index);

    /// <summary>
    /// Gets the identifier of the spawn of a boss with a <see cref="Boss.Spawn"/>.
    /// </summary>
    /// <param name="boss">The boss.</param>
    /// <returns>The identifier.</returns>
    internal static Guid GetSpawnId(Boss boss)
        => GuidHelper.CreateGuid<MonsterSpawnArea>(boss.MapNumber, (short)(FirstSpawnNumber + boss.MonsterNumber));

    private static void ApplySpawn(IContext context, GameMapDefinition map, MonsterDefinition monster, Guid id, BossSpawn position)
    {
        var spawn = map.MonsterSpawns.FirstOrDefault(s => s.GetId() == id);
        if (spawn is null)
        {
            spawn = context.CreateNew<MonsterSpawnArea>();
            spawn.SetGuid(id);
            map.MonsterSpawns.Add(spawn);
        }

        spawn.GameMap = map;
        spawn.MonsterDefinition = monster;
        spawn.Quantity = 1;
        spawn.Direction = Direction.Undefined;
        spawn.SpawnTrigger = SpawnTrigger.Automatic;
        spawn.X1 = position.X;
        spawn.X2 = position.X;
        spawn.Y1 = position.Y;
        spawn.Y2 = position.Y;
    }

    private static void SetAttribute(IContext context, GameConfiguration gameConfiguration, MonsterDefinition monster, AttributeDefinition attributeDefinition, float value)
    {
        var persistentDefinition = attributeDefinition.GetPersistent(gameConfiguration);
        if (monster.Attributes.FirstOrDefault(a => a.AttributeDefinition == persistentDefinition) is { } attribute)
        {
            attribute.Value = value;
        }
        else
        {
            monster.AddAttributes(new Dictionary<AttributeDefinition, float> { { attributeDefinition, value } }, context, gameConfiguration);
        }
    }

    private static void ApplyDropGroup(IContext context, GameConfiguration gameConfiguration, GameMapDefinition map, MonsterDefinition monster, ItemDefinition boxOfLuck, Guid id, string description, byte level)
    {
        var dropGroup = gameConfiguration.DropItemGroups.FirstOrDefault(g => g.GetId() == id);
        if (dropGroup is null)
        {
            dropGroup = context.CreateNew<DropItemGroup>();
            dropGroup.SetGuid(id);
            gameConfiguration.DropItemGroups.Add(dropGroup);
        }

        dropGroup.Description = description;
        dropGroup.Chance = 1.0;
        dropGroup.Monster = monster;
        dropGroup.MinimumMonsterLevel = null;
        dropGroup.MaximumMonsterLevel = null;
        dropGroup.ItemType = SpecialItemType.None;
        dropGroup.ItemLevel = level;
        if (dropGroup.PossibleItems.Count != 1 || !dropGroup.PossibleItems.Contains(boxOfLuck))
        {
            dropGroup.PossibleItems.Clear();
            dropGroup.PossibleItems.Add(boxOfLuck);
        }

        if (!map.DropItemGroups.Contains(dropGroup))
        {
            map.DropItemGroups.Add(dropGroup);
        }
    }

    /// <summary>
    /// A boss with its drops and respawn delay.
    /// </summary>
    /// <param name="MonsterNumber">The number of the monster definition.</param>
    /// <param name="Name">The name, used for the descriptions of the drop groups.</param>
    /// <param name="MapNumber">The number of the map, where the boss drops its items.</param>
    /// <param name="BoxOfKundunPlus">The "+" of the dropped Box of Kundun, from 1 to 5.</param>
    /// <param name="SilverMedals">The number of dropped silver medals.</param>
    /// <param name="RespawnDelay">The delay after which the boss respawns after its death.</param>
    /// <param name="Strength">The strength of the boss.</param>
    /// <param name="Spawn">The position of an additional spawn of the boss on the map, or <c>null</c> if the boss already spawns there in the original data.</param>
    public sealed record Boss(short MonsterNumber, string Name, short MapNumber, byte BoxOfKundunPlus, int SilverMedals, TimeSpan RespawnDelay, BossStrength Strength, BossSpawn? Spawn = null);

    /// <summary>
    /// The position of an additional boss spawn.
    /// </summary>
    /// <param name="X">The x coordinate.</param>
    /// <param name="Y">The y coordinate.</param>
    public sealed record BossSpawn(byte X, byte Y);

    /// <summary>
    /// The strength of a boss, which replaces the values of the original data.
    /// </summary>
    /// <remarks>
    /// The values are meant for a server with resets, where characters get thousands of stat points:
    /// the high defense makes the boss nearly unbeatable for characters without resets, the high health
    /// makes it last minutes instead of seconds for characters with resets, and the high damage and
    /// attack rate make it dangerous for characters which don't care for their defense.
    /// </remarks>
    /// <param name="Health">The maximum health.</param>
    /// <param name="MinimumDamage">The minimum physical damage.</param>
    /// <param name="MaximumDamage">The maximum physical damage.</param>
    /// <param name="Defense">The defense, which is subtracted from the damage of each hit.</param>
    /// <param name="AttackRate">The attack rate, which decides together with the defense rate of the player if the boss hits.</param>
    /// <param name="DefenseRate">The defense rate, which decides together with the attack rate of the player if the player hits.</param>
    public sealed record BossStrength(float Health, float MinimumDamage, float MaximumDamage, float Defense, float AttackRate, float DefenseRate);
}
