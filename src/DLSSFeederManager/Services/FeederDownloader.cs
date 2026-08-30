using DLSSFeederManager.Models;

namespace DLSSFeederManager.Services;

public sealed class FeederDownloader
{
    private static readonly HttpClient Client = new();
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
                using var response = await Client.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var output = File.Create(temporaryPath))
                    await input.CopyToAsync(output, cancellationToken);

                if (!await IsValidAsync(temporaryPath, item.Sha256, cancellationToken))
                {
                    File.Delete(temporaryPath);
                    throw new InvalidDataException($"Hash verification failed for {item.Name}.");
                }

                File.Move(temporaryPath, destination, true);
            }

            files[item] = destination;
        }

        return files;
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
