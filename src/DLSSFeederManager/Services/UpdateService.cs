using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DLSSFeederManager.Services;

public sealed class UpdateService
{
    private const string ReleasesUrl = "https://api.github.com/repos/felipelacerda717/dlss-feeder-manager/releases?per_page=20";
    private const string ExecutableName = "DLSSFeederManager.exe";
    private const string ChecksumName = "DLSSFeederManager.exe.sha256";
    private static readonly Regex HashPattern = new("^[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant);

    public async Task<UpdateRelease?> FindUpdateAsync(CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        using var response = await client.GetAsync(ReleasesUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var current = SemanticVersion.Parse(AppVersion.Current);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.GetProperty("draft").GetBoolean())
                continue;

            var tag = item.GetProperty("tag_name").GetString()?.TrimStart('v', 'V');
            if (!SemanticVersion.TryParse(tag, out var version) || version.CompareTo(current) <= 0)
                continue;

            string? executableUrl = null;
            string? checksumUrl = null;
            foreach (var asset in item.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                var url = asset.GetProperty("browser_download_url").GetString();
                if (name == ExecutableName)
                    executableUrl = url;
                else if (name == ChecksumName)
                    checksumUrl = url;
            }

            if (executableUrl is null || checksumUrl is null)
                continue;

            return new UpdateRelease(
                version.ToString(),
                item.GetProperty("body").GetString() ?? string.Empty,
                executableUrl,
                checksumUrl);
        }

        return null;
    }

    public async Task<string> DownloadAsync(UpdateRelease release, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DLSS Feeder Manager",
            "updates",
            release.Version);
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, ExecutableName);
        var temporaryPath = path + ".download";
        using var client = CreateClient();
        var checksumText = await client.GetStringAsync(release.ChecksumUrl, cancellationToken);
        var expectedHash = checksumText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (expectedHash is null || !HashPattern.IsMatch(expectedHash))
            throw new InvalidDataException("The release checksum is invalid.");

        using (var response = await client.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await input.CopyToAsync(output, cancellationToken);
        }

        var actualHash = await HashFileAsync(temporaryPath, cancellationToken);
        if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(temporaryPath);
            throw new InvalidDataException("The downloaded update failed SHA-256 verification.");
        }

        File.Move(temporaryPath, path, true);
        return path;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"DLSS-Feeder-Manager/{AppVersion.Current}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private static async Task<string> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record SemanticVersion(int Major, int Minor, int Patch, string[] PreRelease) : IComparable<SemanticVersion>
    {
        public static SemanticVersion Parse(string value) =>
            TryParse(value, out var version) ? version : throw new FormatException($"Invalid version: {value}");

        public static bool TryParse(string? value, out SemanticVersion version)
        {
            version = new SemanticVersion(0, 0, 0, []);
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var coreAndPre = value.Split('+', 2)[0].Split('-', 2);
            var core = coreAndPre[0].Split('.');
            if (core.Length != 3 ||
                !int.TryParse(core[0], out var major) ||
                !int.TryParse(core[1], out var minor) ||
                !int.TryParse(core[2], out var patch))
                return false;

            var pre = coreAndPre.Length == 2 ? coreAndPre[1].Split('.') : [];
            version = new SemanticVersion(major, minor, patch, pre);
            return true;
        }

        public int CompareTo(SemanticVersion? other)
        {
            if (other is null)
                return 1;

            var core = Major.CompareTo(other.Major);
            if (core == 0) core = Minor.CompareTo(other.Minor);
            if (core == 0) core = Patch.CompareTo(other.Patch);
            if (core != 0) return core;
            if (PreRelease.Length == 0) return other.PreRelease.Length == 0 ? 0 : 1;
            if (other.PreRelease.Length == 0) return -1;

            for (var index = 0; index < Math.Max(PreRelease.Length, other.PreRelease.Length); index++)
            {
                if (index >= PreRelease.Length) return -1;
                if (index >= other.PreRelease.Length) return 1;
                var leftNumeric = int.TryParse(PreRelease[index], out var left);
                var rightNumeric = int.TryParse(other.PreRelease[index], out var right);
                var result = leftNumeric && rightNumeric
                    ? left.CompareTo(right)
                    : leftNumeric
                        ? -1
                        : rightNumeric
                            ? 1
                            : string.Compare(PreRelease[index], other.PreRelease[index], StringComparison.Ordinal);
                if (result != 0) return result;
            }

            return 0;
        }

        public override string ToString() =>
            $"{Major}.{Minor}.{Patch}" + (PreRelease.Length == 0 ? string.Empty : "-" + string.Join('.', PreRelease));
    }
}
