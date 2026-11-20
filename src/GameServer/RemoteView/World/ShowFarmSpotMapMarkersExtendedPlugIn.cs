// <copyright file="ShowFarmSpotMapMarkersExtendedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.World;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views.World;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The extended implementation of the <see cref="IShowFarmSpotMapMarkersPlugIn"/> which sends the
/// <see cref="FarmSpotMapMarkersExtended"/> packet to the open source client.
/// </summary>
[PlugIn]
[Display(Name = "Show farm spot map markers (extended)", Description = "Sends the farm spots of a map to the open source client, which shows them as dots on its map.")]
[Guid("C51F8A37-92D4-4E6B-8B0A-7E3D14F6C925")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class ShowFarmSpotMapMarkersExtendedPlugIn : IShowFarmSpotMapMarkersPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowFarmSpotMapMarkersExtendedPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowFarmSpotMapMarkersExtendedPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc/>
    public async ValueTask ShowFarmSpotMapMarkersAsync(short mapNumber, IReadOnlyCollection<Point> spots)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var markers = spots.Take(byte.MaxValue).ToList();
        int Write()
        {
            var size = FarmSpotMapMarkersExtendedRef.GetRequiredSize(markers.Count);
            var span = connection.Output.GetSpan(size)[..size];
            var packet = new FarmSpotMapMarkersExtendedRef(span)
            {
                MapNumber = (byte)mapNumber,
                Count = (byte)markers.Count,
            };

            for (var i = 0; i < markers.Count; i++)
            {
                var block = packet[i];
                block.PositionX = markers[i].X;
                block.PositionY = markers[i].Y;
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
