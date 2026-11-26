namespace OldMu.Launcher;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// Starts the game client and checks whether a server is reachable.
/// </summary>
public static class GameStarter
{
    /// <summary>
    /// Starts the client with <c>connect /u&lt;ip&gt; /p&lt;port&gt;</c>, which the client uses instead of its own config.
    /// </summary>
    /// <returns>An error message, or <c>null</c> when the client was started.</returns>
    public static string? Start(LauncherSettings settings)
    {
        var exe = settings.ClientFullPath;
        if (!File.Exists(exe))
        {
            return $"Client not found: {exe}";
        }

        var server = settings.Server;
        if (ResolveHost(server.Host) is not { } ip)
        {
            return $"Server address '{server.Host}' could not be resolved.";
        }

        var startInfo = new ProcessStartInfo(exe)
        {
            WorkingDirectory = Path.GetDirectoryName(exe)!,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("connect");
        startInfo.ArgumentList.Add($"/u{ip}");
        startInfo.ArgumentList.Add($"/p{server.Port}");

        try
        {
            Process.Start(startInfo);
            return null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return $"The client could not be started: {ex.Message}";
        }
    }

    /// <summary>
    /// Checks if the connect server accepts connections.
    /// </summary>
    public static async Task<bool> IsOnlineAsync(ServerEntry server, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            using var client = new TcpClient();
            await client.ConnectAsync(server.Host, server.Port, timeout.Token).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException or ArgumentException)
        {
            return false;
        }
    }

    private static string? ResolveHost(string host)
    {
        if (!IPAddress.TryParse(host, out var address))
        {
            try
            {
                address = Dns.GetHostAddresses(host).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            }
            catch (SocketException)
            {
                return null;
            }
        }

        // The game client refuses 127.0.0.1, any other loopback address works.
        return address is null ? null
            : address.Equals(IPAddress.Loopback) ? "127.127.127.127"
            : address.ToString();
    }
}
