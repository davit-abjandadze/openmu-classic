// <copyright file="ShowBossMapMarkersExtendedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.World;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views.World;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The extended implementation of the <see cref="IShowBossMapMarkersPlugIn"/> which sends the
/// <see cref="BossMapMarkersExtended"/> packet to the open source client.
/// </summary>
[PlugIn]
[Display(Name = "Show boss map markers (extended)", Description = "Sends the bosses of a map to the open source client, which shows them on its map.")]
[Guid("A6D2E41B-0F73-4C8A-B95E-3D17C8F2A064")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class ShowBossMapMarkersExtendedPlugIn : IShowBossMapMarkersPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowBossMapMarkersExtendedPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowBossMapMarkersExtendedPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc/>
    public async ValueTask ShowBossMapMarkersAsync(short mapNumber, IReadOnlyCollection<BossMapMarker> bosses)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var markers = bosses.Take(byte.MaxValue).ToList();
        int Write()
        {
            var size = BossMapMarkersExtendedRef.GetRequiredSize(markers.Count);
            var span = connection.Output.GetSpan(size)[..size];
            var packet = new BossMapMarkersExtendedRef(span)
            {
                MapNumber = (byte)mapNumber,
                Count = (byte)markers.Count,
            };

            for (var i = 0; i < markers.Count; i++)
            {
                var marker = markers[i];
                var block = packet[i];
                block.PositionX = marker.Position.X;
                block.PositionY = marker.Position.Y;
                block.IsAlive = marker.IsAlive;
                block.RespawnSeconds = (uint)Math.Ceiling(Math.Max(0, marker.RespawnIn.TotalSeconds));
                block.Name = marker.Name;
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
