namespace OldMu.Launcher;

using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

internal static class ClientUpdater
{
    private const string PublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAElHJtou/Rbt4syF3ceMc3J2m3s9S9ZTdanl98anPF7QfcBbpqX0xGf/Cgdp59DopA1E0mA6HQDTv61Q8xObxOaw==";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(5) };

    internal const string PendingFile = ".oldmu-update-pending";

    public static async Task<int> ApplyAsync(string website, string root, IProgress<string> progress)
    {
        var feed = new Uri(new Uri(website), "/updates/");
        using var updateLock = new FileStream(Path.Combine(root, ".oldmu-update-lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
        var json = await Client.GetByteArrayAsync(new Uri(feed, "manifest.json")).ConfigureAwait(false);
        var envelope = JsonSerializer.Deserialize<Envelope>(json) ?? throw new InvalidDataException("Empty release.");
        using var signer = ECDsa.Create();
        signer.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKey), out _);
        if (!signer.VerifyData(envelope.Payload, envelope.Signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
        {
            throw new InvalidDataException("Invalid release signature.");
        }

        var files = JsonSerializer.Deserialize<Dictionary<string, string>>(envelope.Payload)
            ?? throw new InvalidDataException("Missing release files.");
        var stage = Directory.CreateTempSubdirectory("oldmu-update-");
        var pending = Path.Combine(root, PendingFile);
        var changes = new List<(string Blob, string Target)>();
        try
        {
            foreach (var (relative, hash) in files)
            {
                if (relative.Split('/').Any(part => part.Length == 0 || part is "." or ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    || hash.Length != 64 || !hash.All(Uri.IsHexDigit))
                {
                    throw new InvalidDataException("Invalid release file.");
                }

                var target = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(target) && Matches(target, hash))
                {
                    continue;
                }

                progress.Report($"Downloading {relative}");
                var blob = Path.Combine(stage.FullName, hash);
                if (!File.Exists(blob))
                {
                    using var download = await Client.GetStreamAsync(new Uri(feed, hash + ".bin")).ConfigureAwait(false);
                    using (var output = File.Create(blob))
                    {
                        await download.CopyToAsync(output).ConfigureAwait(false);
                    }

                    if (!Matches(blob, hash))
                    {
                        throw new InvalidDataException("Downloaded file failed verification.");
                    }
                }

                changes.Add((blob, target));
            }

            if (changes.Count > 0)
            {
                File.WriteAllText(pending, string.Empty);
                foreach (var (blob, target) in changes)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    var temporary = target + ".oldmu-new";
                    File.Copy(blob, temporary, true);
                    File.Move(temporary, target, true);
                }
            }

            File.Delete(pending);
            return changes.Count;
        }
        finally
        {
            stage.Delete(true);
        }
    }

    private static bool Matches(string file, string expected)
    {
        using var stream = File.OpenRead(file);
        return Convert.ToHexString(SHA256.HashData(stream)).Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record Envelope(byte[] Payload, byte[] Signature);
}
