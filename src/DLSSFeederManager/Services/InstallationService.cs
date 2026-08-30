using System.Text;
using System.Text.Json;
using DLSSFeederManager.Models;

namespace DLSSFeederManager.Services;

public sealed class InstallationService
{
    private const string ManagerDirectoryName = ".dlss-feeder-manager";
    private const string ManifestFileName = "install.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly ProfileCatalogService _profiles;
    private readonly SourceCatalogService _sources;
    private readonly FeederDownloader _downloader;

    public InstallationService(
        ProfileCatalogService profiles,
        SourceCatalogService sources,
        FeederDownloader downloader)
    {
        _profiles = profiles;
        _sources = sources;
        _downloader = downloader;
    }

    public OperationResult Check(AppSettings settings)
    {
        var errors = new List<string>();

        if (!File.Exists(settings.GameExecutable))
            errors.Add("Select a game executable.");
        else if (!string.Equals(Path.GetExtension(settings.GameExecutable), ".exe", StringComparison.OrdinalIgnoreCase))
            errors.Add("The selected game file is not an executable.");
        else if (!PeInspector.Is64BitExecutable(settings.GameExecutable))
            errors.Add("The selected executable is not a 64-bit Windows game executable.");

        var gameDirectory = GetGameDirectory(settings.GameExecutable);
        if (gameDirectory is not null)
        {
            if (!File.Exists(Path.Combine(gameDirectory, "dxgi.dll")))
                errors.Add("ReShade with add-on support was not found next to the game executable.");

            if (!Directory.Exists(Path.Combine(gameDirectory, "reshade-shaders")))
                errors.Add("The reshade-shaders directory was not found.");

            if (File.Exists(GetManifestPath(gameDirectory)))
                errors.Add("This game already has an installation managed by this application.");
        }

        ValidateFile(settings.RenoDxAddon, ".addon64", "Select the RenoDX DLSS 5 add-on.", errors);
        ValidateNamedFile(settings.DlssRuntime, "nvngx_dlss.dll", errors);
        ValidateNamedFile(settings.DlssNrRuntime, "nvngx_dlssnr.dll", errors);

        try
        {
            using var package = ImmersePackage.Open(settings.ImmerseSource);
        }
        catch (Exception exception)
        {
            errors.Add(exception.Message);
        }

        if (errors.Count > 0)
            return OperationResult.Fail("The installation is not ready.", errors.ToArray());

        var profile = _profiles.FindByExecutable(settings.GameExecutable);
        var release = _sources.GetFeederRelease(profile);
        return OperationResult.Ok(
            "Ready to install.",
            profile is null ? "Profile: Generic (experimental)" : $"Profile: {profile.Name}",
            $"DLSS5-Feeder: v{release.Version}");
    }

    public async Task<OperationResult> InstallAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        var check = Check(settings);
        if (!check.Success)
            return check;

        var gameDirectory = GetGameDirectory(settings.GameExecutable)!;
        var profile = _profiles.FindByExecutable(settings.GameExecutable);
        var release = _sources.GetFeederRelease(profile);
        var managerDirectory = Path.Combine(gameDirectory, ManagerDirectoryName);
        var backupId = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
        var backupDirectory = Path.Combine(managerDirectory, "backups", backupId);
        var manifest = new InstallManifest
        {
            GameExecutable = Path.GetFileName(settings.GameExecutable),
            ProfileId = profile?.Id ?? "generic",
            FeederRelease = release.Version,
            InstalledAt = DateTimeOffset.UtcNow
        };

        try
        {
            var feederFiles = await _downloader.GetAsync(release, cancellationToken);
            using var immerse = ImmersePackage.Open(settings.ImmerseSource);
            var operations = BuildOperations(settings, release, feederFiles, immerse);

            Directory.CreateDirectory(backupDirectory);

            foreach (var operation in operations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destination = ResolveDestination(gameDirectory, operation.RelativeDestination);
                var entry = new InstalledFile
                {
                    RelativePath = operation.RelativeDestination,
                    HadOriginal = File.Exists(destination)
                };

                if (entry.HadOriginal)
                {
                    entry.BackupRelativePath = Path.Combine("backups", backupId, operation.RelativeDestination);
                    var backupPath = ResolveDestination(managerDirectory, entry.BackupRelativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                    File.Copy(destination, backupPath, true);
                }

                manifest.Files.Add(entry);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(operation.Source, destination, true);
            }

            await SaveManifestAsync(GetManifestPath(gameDirectory), manifest, cancellationToken);
            return OperationResult.Ok(
                "Installation completed.",
                profile is null ? "Generic experimental mode" : profile.Name,
                $"DLSS5-Feeder v{release.Version}",
                $"{manifest.Files.Count} files installed");
        }
        catch (Exception exception)
        {
            try
            {
                RestoreFiles(gameDirectory, manifest.Files);
                TryDeleteDirectory(managerDirectory);
                return OperationResult.Fail("Installation failed and changes were rolled back.", exception.Message);
            }
            catch (Exception rollbackException)
            {
                return OperationResult.Fail(
                    "Installation failed and rollback needs attention.",
                    exception.Message,
                    rollbackException.Message,
                    $"Backup directory: {backupDirectory}");
            }
        }
    }

    public async Task<OperationResult> RemoveAsync(
        string gameExecutable,
        CancellationToken cancellationToken = default)
    {
        var gameDirectory = GetGameDirectory(gameExecutable);
        if (gameDirectory is null)
            return OperationResult.Fail("Select the game executable.");

        var manifestPath = GetManifestPath(gameDirectory);
        if (!File.Exists(manifestPath))
            return OperationResult.Fail("No managed installation was found.");

        try
        {
            await using var stream = File.OpenRead(manifestPath);
            var manifest = await JsonSerializer.DeserializeAsync<InstallManifest>(
                stream,
                JsonOptions,
                cancellationToken)
                ?? throw new InvalidDataException("The installation manifest is invalid.");

            RestoreFiles(gameDirectory, manifest.Files);
            TryDeleteDirectory(Path.Combine(gameDirectory, ManagerDirectoryName));
            return OperationResult.Ok("The installation was removed and original files were restored.");
        }
        catch (Exception exception)
        {
            return OperationResult.Fail("Removal failed.", exception.Message);
        }
    }

    public async Task<OperationResult> ValidateAsync(
        string gameExecutable,
        CancellationToken cancellationToken = default)
    {
        var gameDirectory = GetGameDirectory(gameExecutable);
        if (gameDirectory is null)
            return OperationResult.Fail("Select the game executable.");

        var manifestPath = GetManifestPath(gameDirectory);
        if (!File.Exists(manifestPath))
            return OperationResult.Fail("No managed installation was found.");

        try
        {
            await using var stream = File.OpenRead(manifestPath);
            var manifest = await JsonSerializer.DeserializeAsync<InstallManifest>(
                stream,
                JsonOptions,
                cancellationToken)
                ?? throw new InvalidDataException("The installation manifest is invalid.");

            var missing = manifest.Files
                .Where(file => !File.Exists(ResolveDestination(gameDirectory, file.RelativePath)))
                .Select(file => file.RelativePath)
                .ToArray();

            if (missing.Length > 0)
                return OperationResult.Fail("Installed files are missing.", missing);

            var details = new List<string> { $"{manifest.Files.Count} installed files found" };
            var feederLog = Path.Combine(gameDirectory, "dlss5-feed.log");
            var reshadeLog = Path.Combine(gameDirectory, "ReShade.log");

            if (!File.Exists(feederLog) || !File.Exists(reshadeLog))
            {
                details.Add("Runtime validation pending: launch the game, then validate again.");
                return OperationResult.Ok("The installed file layout is valid.", details.ToArray());
            }

            var feederText = await ReadSharedTextAsync(feederLog, cancellationToken);
            var reshadeText = await ReadSharedTextAsync(reshadeLog, cancellationToken);
            var feederReady = feederText.Contains("feature ready", StringComparison.OrdinalIgnoreCase)
                && feederText.Contains("delivered", StringComparison.OrdinalIgnoreCase);
            var neuralReady = reshadeText.Contains("feature 18 created", StringComparison.OrdinalIgnoreCase)
                && reshadeText.Contains("inline feature 18 evaluation succeeded", StringComparison.OrdinalIgnoreCase);

            if (!feederReady || !neuralReady)
            {
                if (!feederReady)
                    details.Add("DLSS5-Feeder runtime markers were not found in dlss5-feed.log.");
                if (!neuralReady)
                    details.Add("Neural rendering markers were not found in ReShade.log.");
                return OperationResult.Fail("The runtime has not been validated.", details.ToArray());
            }

            details.Add("DLSS5-Feeder runtime confirmed");
            details.Add("DLSS 5 neural rendering confirmed");
            return OperationResult.Ok("Installation and runtime validation passed.", details.ToArray());
        }
        catch (Exception exception)
        {
            return OperationResult.Fail("Validation failed.", exception.Message);
        }
    }

    private static List<CopyOperation> BuildOperations(
        AppSettings settings,
        FeederRelease release,
        IReadOnlyDictionary<DownloadFile, string> feederFiles,
        ImmersePackage immerse)
    {
        var operations = release.Files
            .Select(file => new CopyOperation(feederFiles[file], NormalizeRelativePath(file.Destination)))
            .ToList();

        operations.Add(new CopyOperation(settings.RenoDxAddon, Path.GetFileName(settings.RenoDxAddon)));
        operations.Add(new CopyOperation(settings.DlssRuntime, Path.GetFileName(settings.DlssRuntime)));
        operations.Add(new CopyOperation(settings.DlssNrRuntime, Path.GetFileName(settings.DlssNrRuntime)));
        operations.Add(new CopyOperation(
            immerse.LaunchPadFile,
            Path.Combine("reshade-shaders", "Shaders", "MartysMods_LAUNCHPAD.fx")));
        operations.Add(new CopyOperation(
            immerse.BlueNoiseFile,
            Path.Combine("reshade-shaders", "Textures", "iMMERSE_bluenoise_opt.png")));

        foreach (var file in Directory.EnumerateFiles(immerse.MartysModsDirectory, "*", SearchOption.AllDirectories))
        {
            operations.Add(new CopyOperation(
                file,
                Path.Combine(
                    "reshade-shaders",
                    "Shaders",
                    "MartysMods",
                    Path.GetRelativePath(immerse.MartysModsDirectory, file))));
        }

        return operations
            .GroupBy(operation => operation.RelativeDestination, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Single())
            .ToList();
    }

    private static void RestoreFiles(string gameDirectory, IEnumerable<InstalledFile> files)
    {
        var managerDirectory = Path.Combine(gameDirectory, ManagerDirectoryName);

        foreach (var file in files.Reverse())
        {
            var destination = ResolveDestination(gameDirectory, file.RelativePath);
            if (file.HadOriginal && file.BackupRelativePath is not null)
            {
                var backup = ResolveDestination(managerDirectory, file.BackupRelativePath);
                if (!File.Exists(backup))
                    throw new FileNotFoundException($"Backup not found for {file.RelativePath}.", backup);

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(backup, destination, true);
            }
            else if (File.Exists(destination))
            {
                File.Delete(destination);
            }
        }
    }

    private static async Task SaveManifestAsync(
        string path,
        InstallManifest manifest,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, cancellationToken);
    }

    private static async Task<string> ReadSharedTextAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static void ValidateFile(
        string path,
        string extension,
        string missingMessage,
        ICollection<string> errors)
    {
        if (!File.Exists(path))
            errors.Add(missingMessage);
        else if (!string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase))
            errors.Add($"Unexpected file type: {Path.GetFileName(path)}");
    }

    private static void ValidateNamedFile(string path, string expectedName, ICollection<string> errors)
    {
        if (!File.Exists(path))
            errors.Add($"Select {expectedName}.");
        else if (!string.Equals(Path.GetFileName(path), expectedName, StringComparison.OrdinalIgnoreCase))
            errors.Add($"The selected file must be named {expectedName}.");
    }

    private static string? GetGameDirectory(string gameExecutable)
    {
        if (string.IsNullOrWhiteSpace(gameExecutable))
            return null;
        return Path.GetDirectoryName(gameExecutable);
    }

    private static string GetManifestPath(string gameDirectory) =>
        Path.Combine(gameDirectory, ManagerDirectoryName, ManifestFileName);

    private static string NormalizeRelativePath(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

    private static string ResolveDestination(string root, string relativePath)
    {
        var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var destination = Path.GetFullPath(Path.Combine(root, NormalizeRelativePath(relativePath)));
        if (!destination.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Path escapes the game directory: {relativePath}");
        return destination;
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

    private sealed record CopyOperation(string Source, string RelativeDestination);
}
