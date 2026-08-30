using System.IO.Compression;

namespace DLSSFeederManager.Services;

public sealed class ImmersePackage : IDisposable
{
    private readonly string? _temporaryDirectory;

    private ImmersePackage(
        string launchPadFile,
        string martysModsDirectory,
        string blueNoiseFile,
        string? temporaryDirectory)
    {
        LaunchPadFile = launchPadFile;
        MartysModsDirectory = martysModsDirectory;
        BlueNoiseFile = blueNoiseFile;
        _temporaryDirectory = temporaryDirectory;
    }

    public string LaunchPadFile { get; }
    public string MartysModsDirectory { get; }
    public string BlueNoiseFile { get; }

    public static ImmersePackage Open(string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("iMMERSE source was not found.", sourcePath);

        string searchRoot;
        string? temporaryDirectory = null;

        if (string.Equals(Path.GetExtension(sourcePath), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "DLSSFeederManager",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
            ZipFile.ExtractToDirectory(sourcePath, temporaryDirectory);
            searchRoot = temporaryDirectory;
        }
        else
        {
            if (!string.Equals(
                    Path.GetFileName(sourcePath),
                    "MartysMods_LAUNCHPAD.fx",
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Select the iMMERSE ZIP or MartysMods_LAUNCHPAD.fx.");

            searchRoot = Directory.GetParent(Path.GetDirectoryName(sourcePath)!)?.FullName
                ?? Path.GetDirectoryName(sourcePath)!;
        }

        try
        {
            var launchPad = Directory.EnumerateFiles(
                    searchRoot,
                    "MartysMods_LAUNCHPAD.fx",
                    SearchOption.AllDirectories)
                .FirstOrDefault()
                ?? throw new InvalidDataException("MartysMods_LAUNCHPAD.fx was not found.");

            var martysMods = Path.Combine(Path.GetDirectoryName(launchPad)!, "MartysMods");
            if (!Directory.Exists(martysMods))
                throw new InvalidDataException("The iMMERSE MartysMods directory was not found.");

            var blueNoise = Directory.EnumerateFiles(
                    searchRoot,
                    "iMMERSE_bluenoise_opt.png",
                    SearchOption.AllDirectories)
                .FirstOrDefault()
                ?? throw new InvalidDataException("iMMERSE_bluenoise_opt.png was not found.");

            return new ImmersePackage(launchPad, martysMods, blueNoise, temporaryDirectory);
        }
        catch
        {
            if (temporaryDirectory is not null)
                Directory.Delete(temporaryDirectory, true);
            throw;
        }
    }

    public void Dispose()
    {
        if (_temporaryDirectory is null || !Directory.Exists(_temporaryDirectory))
            return;

        try
        {
            Directory.Delete(_temporaryDirectory, true);
        }
        catch
        {
        }
    }
}
