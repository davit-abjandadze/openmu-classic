namespace OldMu.Launcher;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Settings of the launcher, stored as <c>launcher.json</c> next to the executable.
/// </summary>
public sealed class LauncherSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Gets the path of the settings file.</summary>
    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "launcher.json");

    /// <summary>Gets or sets the servers which can be selected.</summary>
    public List<ServerEntry> Servers { get; set; } = [new()];

    /// <summary>Gets or sets the index of the selected server.</summary>
    public int SelectedServer { get; set; }

    /// <summary>Gets or sets the path to the client's Main.exe, absolute or relative to the launcher.</summary>
    public string ClientPath { get; set; } = @"Client\Main.exe";

    /// <summary>Gets or sets the website address.</summary>
    public string WebsiteUrl { get; set; } = "https://example.com";

    /// <summary>Gets or sets the discord invitation address.</summary>
    public string DiscordUrl { get; set; } = "https://discord.com";

    /// <summary>Gets or sets a value indicating whether the launcher closes after the game started.</summary>
    public bool CloseAfterStart { get; set; } = true;

    /// <summary>Gets the selected server.</summary>
    [JsonIgnore]
    public ServerEntry Server
    {
        get
        {
            // "Servers": null in the file deserializes to null.
            if (this.Servers is not { Count: > 0 })
            {
                this.Servers = [new ServerEntry()];
            }

            return this.Servers[Math.Clamp(this.SelectedServer, 0, this.Servers.Count - 1)];
        }
    }

    /// <summary>Gets the full path of the client executable.</summary>
    [JsonIgnore]
    public string ClientFullPath => Path.GetFullPath(this.ClientPath, AppContext.BaseDirectory);

    /// <summary>Loads the settings, or returns the defaults when there are none.</summary>
    public static LauncherSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(FilePath), JsonOptions) ?? new();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            MessageBox.Show($"launcher.json could not be read, defaults are used.{Environment.NewLine}{ex.Message}", "Old MU");

            // The file isn't overwritten with the defaults, so that a typo doesn't wipe the configured servers.
            return new LauncherSettings();
        }

        var settings = new LauncherSettings();
        settings.Save();
        return settings;
    }

    /// <summary>Saves the settings.</summary>
    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"launcher.json could not be saved.{Environment.NewLine}{ex.Message}", "Old MU");
        }
    }
}

/// <summary>
/// A server the launcher can connect the client to.
/// </summary>
public sealed class ServerEntry
{
    /// <summary>Gets or sets the displayed name.</summary>
    public string Name { get; set; } = "Old Mu Server";

    /// <summary>Gets or sets the host name or IP address of the connect server.</summary>
    /// <remarks>The client refuses 127.0.0.1, so a local server is reached through 127.127.127.127.</remarks>
    public string Host { get; set; } = "127.127.127.127";

    /// <summary>Gets or sets the port of the connect server (44406 for the open source client).</summary>
    public int Port { get; set; } = 44406;
}
