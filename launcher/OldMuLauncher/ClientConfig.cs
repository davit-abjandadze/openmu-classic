namespace OldMu.Launcher;

/// <summary>
/// The game options in the client's <c>config.ini</c>.
/// </summary>
/// <remarks>
/// The client keeps <c>config.ini</c> in <c>%APPDATA%\MuMain\&lt;Name&gt;</c>, where the name comes from
/// <c>[Client] Name</c> of the <c>config.ini.template</c> next to <c>Main.exe</c>. Without a name the client
/// derives the folder from its install path, so the launcher sets a fixed one to be able to find the file.
/// When the file doesn't exist yet, the client creates it from the template, and so does the launcher.
/// </remarks>
public sealed class ClientConfig
{
    private const string DefaultFolderName = "OldMu";
    private const string TemplateFileName = "config.ini.template";
    private const int DefaultMusicVolume = 5;

    /// <summary>
    /// The resolutions offered in the settings.
    /// </summary>
    public static readonly IReadOnlyList<Size> Resolutions =
    [
        new(800, 600),
        new(1024, 768),
        new(1280, 720),
        new(1280, 800),
        new(1280, 1024),
        new(1366, 768),
        new(1440, 900),
        new(1600, 900),
        new(1680, 1050),
        new(1920, 1080),
        new(2560, 1440),
    ];

    private readonly string filePath;
    private readonly List<string> lines;

    private ClientConfig(string filePath, List<string> lines)
    {
        this.filePath = filePath;
        this.lines = lines;
        this.Resolution = new Size(this.GetInt("Window", "Width", 1024), this.GetInt("Window", "Height", 768));
        this.FullScreen = this.GetInt("Window", "Windowed", 1) == 0;
        this.RememberAccount = this.GetInt("LOGIN", "RememberMe", 0) != 0;
        this.Music = this.GetInt("Audio", "MusicVolume", DefaultMusicVolume) > 0;
    }

    /// <summary>Gets or sets the size of the game window, or of the screen in full screen mode.</summary>
    public Size Resolution { get; set; }

    /// <summary>Gets or sets a value indicating whether the game runs in full screen instead of a window.</summary>
    public bool FullScreen { get; set; }

    /// <summary>Gets or sets a value indicating whether the login screen remembers the last account.</summary>
    /// <remarks>The client stores the account itself, encrypted, when logging in with "remember me".</remarks>
    public bool RememberAccount { get; set; }

    /// <summary>Gets or sets a value indicating whether the music plays in the game.</summary>
    public bool Music { get; set; }

    /// <summary>Reads the options of the client at <paramref name="clientExePath"/>.</summary>
    /// <returns>The options, or <c>null</c> with an error message when they can't be read.</returns>
    public static (ClientConfig? Config, string? Error) Load(string clientExePath)
    {
        try
        {
            var clientDirectory = Path.GetDirectoryName(clientExePath)!;
            var templatePath = Path.Combine(clientDirectory, TemplateFileName);
            var template = File.Exists(templatePath) ? File.ReadAllLines(templatePath).ToList() : null;
            if (template is null)
            {
                return (null, $"{TemplateFileName} not found next to {clientExePath}.");
            }

            var folderName = GetValue(template, "Client", "Name");
            if (string.IsNullOrWhiteSpace(folderName))
            {
                folderName = DefaultFolderName;
                SetValue(template, "Client", "Name", folderName);
                File.WriteAllLines(templatePath, template);
            }

            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MuMain", folderName.Trim(), "config.ini");
            var lines = File.Exists(filePath) ? File.ReadAllLines(filePath).ToList() : template;
            return (new ClientConfig(filePath, lines), null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (null, $"The game options could not be read: {ex.Message}");
        }
    }

    /// <summary>Writes the options, so the client uses them on its next start.</summary>
    /// <returns>An error message, or <c>null</c> when the options were saved.</returns>
    public string? Save()
    {
        SetValue(this.lines, "Window", "Width", this.Resolution.Width.ToString());
        SetValue(this.lines, "Window", "Height", this.Resolution.Height.ToString());
        SetValue(this.lines, "Window", "Windowed", this.FullScreen ? "0" : "1");
        SetValue(this.lines, "LOGIN", "RememberMe", this.RememberAccount ? "1" : "0");
        if (!this.RememberAccount)
        {
            SetValue(this.lines, "LOGIN", "EncryptedUsername", string.Empty);
        }

        var volume = this.GetInt("Audio", "MusicVolume", DefaultMusicVolume);
        SetValue(this.lines, "Audio", "MusicVolume", (this.Music ? (volume > 0 ? volume : DefaultMusicVolume) : 0).ToString());

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(this.filePath)!);
            File.WriteAllLines(this.filePath, this.lines);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"The game options could not be saved: {ex.Message}";
        }
    }

    private static string? GetValue(List<string> lines, string section, string key)
    {
        var (_, keyIndex) = Find(lines, section, key);
        return keyIndex < 0 ? null : lines[keyIndex][(lines[keyIndex].IndexOf('=') + 1)..].Trim();
    }

    private static void SetValue(List<string> lines, string section, string key, string value)
    {
        var (sectionEnd, keyIndex) = Find(lines, section, key);
        if (keyIndex >= 0)
        {
            lines[keyIndex] = $"{key}={value}";
        }
        else if (sectionEnd >= 0)
        {
            lines.Insert(sectionEnd, $"{key}={value}");
        }
        else
        {
            lines.Add($"[{section}]");
            lines.Add($"{key}={value}");
        }
    }

    /// <summary>
    /// Finds a key in an ini file.
    /// </summary>
    /// <returns>The index after the section's last entry (-1 without the section), and the index of the key (-1 when missing).</returns>
    private static (int SectionEnd, int KeyIndex) Find(List<string> lines, string section, string key)
    {
        var inSection = false;
        var sectionEnd = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                inSection = string.Equals(line[1..^1], section, StringComparison.OrdinalIgnoreCase);
                if (inSection)
                {
                    sectionEnd = i + 1;
                }

                continue;
            }

            if (!inSection || line.Length == 0 || line.StartsWith(';'))
            {
                continue;
            }

            sectionEnd = i + 1;
            var separator = line.IndexOf('=');
            if (separator > 0 && string.Equals(line[..separator].Trim(), key, StringComparison.OrdinalIgnoreCase))
            {
                return (sectionEnd, i);
            }
        }

        return (sectionEnd, -1);
    }

    private int GetInt(string section, string key, int defaultValue)
        => int.TryParse(GetValue(this.lines, section, key), out var value) ? value : defaultValue;
}
