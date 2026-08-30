using System.IO.Compression;
using System.Reflection;
using System.Text;
using DLSSFeederManager.Models;

namespace DLSSFeederManager.Services;

public sealed class FeederDownloader
{
    private const string EmbeddedFolderMarker = ".Assets.EmbeddedFeeder.";
    private const string AddonPartsPrefix = "dlss5-feed.addon64.gz.b64.part";

    private readonly string _cacheRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DLSS Feeder Manager",
        "cache");

    public async Task<IReadOnlyDictionary<DownloadFile, string>> GetAsync(
        FeederRelease release,
        CancellationToken cancellationToken = default)
    {
        var releaseDirectory = Path.Combine(_cacheRoot, "feeder", release.Version);
        Directory.CreateDirectory(releaseDirectory);
        var files = new Dictionary<DownloadFile, string>();

        foreach (var item in release.Files)
        {
            var destination = Path.Combine(releaseDirectory, item.Name);
            if (!await IsValidAsync(destination, item.Sha256, cancellationToken))
            {
                var temporaryPath = destination + ".download";
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                await ExtractEmbeddedFileAsync(item.Name, temporaryPath, cancellationToken);

                if (!await IsValidAsync(temporaryPath, item.Sha256, cancellationToken))
                {
                    File.Delete(temporaryPath);
                    throw new InvalidDataException(
                        $"Embedded DLSS5-Feeder hash verification failed for {item.Name}.");
                }

                File.Move(temporaryPath, destination, true);
            }

            files[item] = destination;
        }

        return files;
    }

    private static async Task ExtractEmbeddedFileAsync(
        string fileName,
        string destination,
        CancellationToken cancellationToken)
    {
        if (string.Equals(fileName, "dlss5-feed.addon64", StringComparison.OrdinalIgnoreCase))
        {
            await ExtractAddonAsync(destination, cancellationToken);
            return;
        }

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(name =>
                name.Contains(EmbeddedFolderMarker, StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Embedded feeder resource not found: {fileName}");

        await using var input = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded feeder resource could not be opened: {fileName}");
        await using var output = File.Create(destination);
        await input.CopyToAsync(output, cancellationToken);
    }

    private static async Task ExtractAddonAsync(
        string destination,
        CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var parts = assembly.GetManifestResourceNames()
            .Where(name =>
                name.Contains(EmbeddedFolderMarker, StringComparison.OrdinalIgnoreCase)
                && name.Contains(AddonPartsPrefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (parts.Length == 0)
            throw new InvalidOperationException("Embedded dlss5-feed.addon64 payload was not found.");

        var encoded = new StringBuilder();
        foreach (var part in parts)
        {
            await using var stream = assembly.GetManifestResourceStream(part)
                ?? throw new InvalidOperationException($"Embedded feeder payload part could not be opened: {part}");
            using var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, leaveOpen: false);
            encoded.Append(await reader.ReadToEndAsync(cancellationToken));
        }

        byte[] compressed;
        try
        {
            compressed = Convert.FromBase64String(encoded.ToString());
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("Embedded dlss5-feed.addon64 payload is invalid.", exception);
        }

        await using var compressedStream = new MemoryStream(compressed, writable: false);
        await using var gzip = new GZipStream(compressedStream, CompressionMode.Decompress);
        await using var output = File.Create(destination);
        await gzip.CopyToAsync(output, cancellationToken);
    }

    private static async Task<bool> IsValidAsync(
        string path,
        string expectedHash,
        CancellationToken cancellationToken)
    {
        return File.Exists(path)
            && string.Equals(
                await HashService.Sha256Async(path, cancellationToken),
                expectedHash,
                StringComparison.OrdinalIgnoreCase);
    }
}
