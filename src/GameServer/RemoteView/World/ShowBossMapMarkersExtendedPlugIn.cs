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
    /// <summary>
    /// The length in bytes (UTF-8) of the name field of a marker. Has to match the BossMarker structure of the packet definition.
    /// </summary>
    internal const int NameLength = 20;

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

            // The buffer is pooled, and the markers have an unused byte, which shouldn't contain old data.
            span.Clear();
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
                block.Name = FitName(marker.Name);
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <summary>
    /// Cuts the name to the size of the name field of a marker. Writing a longer name would throw
    /// and the markers of the whole map wouldn't be sent.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The name, which fits into <see cref="NameLength"/> bytes.</returns>
    internal static string FitName(string name)
    {
        var length = name.Length;
        while (length > 0 && Encoding.UTF8.GetByteCount(name.AsSpan(0, length)) > NameLength)
        {
            length--;
        }

        if (length > 0 && length < name.Length && char.IsHighSurrogate(name[length - 1]))
        {
            length--;
        }

        return length == name.Length ? name : name[..length];
    }
}
