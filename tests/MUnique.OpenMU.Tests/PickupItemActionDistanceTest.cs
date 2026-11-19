// <copyright file="PickupItemActionDistanceTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.GameServer.MessageHandler;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Tests the distance check of <see cref="PickupItemAction"/>.
/// </summary>
[TestFixture]
internal class PickupItemActionDistanceTest
{
    private const uint Amount = 1000;

    private static readonly Point StartPoint = new(147, 120);

    /// <summary>
    /// Tests that a drop at the end of a walk can be picked up while the server is still a step
    /// behind the client on that walk. The client sends the request as soon as it arrived.
    /// </summary>
    [Test]
    public async ValueTask DropCanBePickedUpWhileServerIsBehindOnTheWalkAsync()
    {
        var player = await CreatePlayerAsync().ConfigureAwait(false);
        var packet = new byte[] { 0xC1, 0x08, (byte)PacketType.Walk, StartPoint.X, StartPoint.Y, 0x44, 0x33, 0x44 };
        await new CharacterWalkHandlerPlugIn().HandlePacketAsync(player, packet).ConfigureAwait(false);
        Assert.That(player.IsWalking, Is.True);

        // The drop is just out of range of where the server currently is, in the direction of the
        // walk. Further steps of the walker only bring the player closer.
        var drop = await DropMoneyAsync(player).ConfigureAwait(false);
        var moneyBefore = player.Money;

        await new PickupItemAction().PickupItemAsync(player, drop.Id).ConfigureAwait(false);

        Assert.That(player.Money, Is.GreaterThan(moneyBefore));
    }

    /// <summary>
    /// Tests that a drop out of range is refused when the player isn't walking, so the tolerance for
    /// the walk lag doesn't widen the pick up distance in general.
    /// </summary>
    [Test]
    public async ValueTask DropOutOfRangeIsRefusedWhenNotWalkingAsync()
    {
        var player = await CreatePlayerAsync().ConfigureAwait(false);
        Assert.That(player.IsWalking, Is.False);
        var drop = await DropMoneyAsync(player).ConfigureAwait(false);
        var moneyBefore = player.Money;

        await new PickupItemAction().PickupItemAsync(player, drop.Id).ConfigureAwait(false);

        Assert.That(player.Money, Is.EqualTo(moneyBefore));
    }

    /// <summary>
    /// Tests that a drop within the pick up distance is picked up.
    /// </summary>
    [Test]
    public async ValueTask DropInRangeIsPickedUpAsync()
    {
        var player = await CreatePlayerAsync().ConfigureAwait(false);
        var drop = await DropMoneyAsync(player, PickupItemAction.MaximumPickupDistance).ConfigureAwait(false);
        var moneyBefore = player.Money;

        await new PickupItemAction().PickupItemAsync(player, drop.Id).ConfigureAwait(false);

        Assert.That(player.Money, Is.GreaterThan(moneyBefore));
    }

    private static async ValueTask<Player> CreatePlayerAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.GameContext.Configuration.MaximumInventoryMoney = int.MaxValue;
        player.Money = 0;
        player.Position = StartPoint;
        return player;
    }

    private static async ValueTask<DroppedMoney> DropMoneyAsync(Player player, int distance = PickupItemAction.MaximumPickupDistance + 1)
    {
        var position = new Point((byte)(player.Position.X + distance), player.Position.Y);
        var drop = new DroppedMoney(Amount, position, player.CurrentMap!);
        await player.CurrentMap!.AddAsync(drop).ConfigureAwait(false);
        return drop;
    }
}
