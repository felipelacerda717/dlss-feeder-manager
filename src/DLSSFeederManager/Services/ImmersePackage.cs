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

        if (string.Equals(Path.GetExtension(sourcePath), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            var temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "DLSSFeederManager",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
            try
            {
                ZipFile.ExtractToDirectory(sourcePath, temporaryDirectory);
                return OpenExtractedPackage(temporaryDirectory, temporaryDirectory);
            }
            catch
            {
                TryDeleteDirectory(temporaryDirectory);
                throw;
            }
        }

        if (!string.Equals(
                Path.GetFileName(sourcePath),
                "MartysMods_LAUNCHPAD.fx",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Select the complete iMMERSE ZIP or MartysMods_LAUNCHPAD.fx.");

        var shadersDirectory = Path.GetDirectoryName(sourcePath)!;
        if (!string.Equals(
                Path.GetFileName(shadersDirectory),
                "Shaders",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(
                "Keep MartysMods_LAUNCHPAD.fx inside the original iMMERSE Shaders folder, or select the complete ZIP.");

        return OpenLayout(sourcePath, shadersDirectory, null);
    }

    private static ImmersePackage OpenExtractedPackage(string root, string temporaryDirectory)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };
        var launchPads = Directory.EnumerateFiles(root, "*", options)
            .Where(path => string.Equals(
                Path.GetFileName(path),
                "MartysMods_LAUNCHPAD.fx",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (launchPads.Length == 0)
            throw new InvalidDataException(
                "The selected ZIP does not contain Shaders\\MartysMods_LAUNCHPAD.fx.");

        foreach (var launchPad in launchPads)
        {
            var shadersDirectory = Path.GetDirectoryName(launchPad)!;
            if (!string.Equals(
                    Path.GetFileName(shadersDirectory),
                    "Shaders",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            var packageRoot = Directory.GetParent(shadersDirectory)?.FullName;
            if (packageRoot is null)
                continue;

            var martysMods = Path.Combine(shadersDirectory, "MartysMods");
            var blueNoise = Path.Combine(packageRoot, "Textures", "iMMERSE_bluenoise_opt.png");
            if (Directory.Exists(martysMods) && File.Exists(blueNoise))
                return new ImmersePackage(launchPad, martysMods, blueNoise, temporaryDirectory);
        }

        throw new InvalidDataException(
            "The selected ZIP is incomplete. It must contain the original Shaders, Shaders\\MartysMods, and Textures folders.");
    }

    private static ImmersePackage OpenLayout(
        string launchPad,
        string shadersDirectory,
        string? temporaryDirectory)
    {
        var martysMods = Path.Combine(shadersDirectory, "MartysMods");
        if (!Directory.Exists(martysMods))
            throw new InvalidDataException(
                $"The iMMERSE folder is missing: {martysMods}");

        var packageRoot = Directory.GetParent(shadersDirectory)?.FullName
            ?? throw new InvalidDataException("The iMMERSE package root could not be determined.");
        var blueNoise = Path.Combine(packageRoot, "Textures", "iMMERSE_bluenoise_opt.png");
        if (!File.Exists(blueNoise))
            throw new InvalidDataException(
                $"The iMMERSE texture is missing: {blueNoise}");

        return new ImmersePackage(launchPad, martysMods, blueNoise, temporaryDirectory);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        if (_temporaryDirectory is null || !Directory.Exists(_temporaryDirectory))
            return;

        TryDeleteDirectory(_temporaryDirectory);
    }
}
