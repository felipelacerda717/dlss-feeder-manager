using System.Diagnostics;
using System.Windows;

namespace DLSSFeederManager.Services;

public static class UpdateBootstrap
{
    public sealed record Completion(string BackupPath, string MarkerPath);

    public static void Start(string updatePath, string currentPath)
    {
        var token = Guid.NewGuid().ToString("N");
        var markerPath = Path.Combine(Path.GetDirectoryName(updatePath)!, token + ".ready");
        Process.Start(new ProcessStartInfo
        {
            FileName = updatePath,
            UseShellExecute = false,
            ArgumentList =
            {
                "--apply-update",
                Path.GetFullPath(currentPath),
                Environment.ProcessId.ToString(),
                markerPath
            }
        });
    }

    public static Completion? ParseCompletion(string[] args)
    {
        if (args.Length != 3 ||
            args[0] != "--complete-update" ||
            !TryFullPath(args[1], out var backupPath) ||
            !backupPath.EndsWith(".exe.previous", StringComparison.OrdinalIgnoreCase) ||
            !IsSafeMarker(args[2], out var markerPath))
            return null;

        return new Completion(backupPath, markerPath);
    }

    public static async Task CompleteAsync(Completion completion)
    {
        try
        {
            await File.WriteAllTextAsync(completion.MarkerPath, "ready");
        }
        catch
        {
        }
    }

    public static async Task ApplyAsync(string[] args)
    {
        if (args.Length != 3 ||
            !int.TryParse(args[1], out var processId) ||
            !IsSafeTarget(args[0], out var targetPath) ||
            !IsSafeMarker(args[2], out var markerPath))
        {
            MessageBox.Show("The update request is invalid.", "DLSS Feeder Manager", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var backupPath = targetPath + ".previous";
        try
        {
            await WaitForExitAsync(processId);
            File.Copy(targetPath, backupPath, true);
            File.Copy(Environment.ProcessPath!, targetPath, true);

            using var updated = Process.Start(new ProcessStartInfo
            {
                FileName = targetPath,
                UseShellExecute = false,
                ArgumentList = { "--complete-update", backupPath, markerPath }
            }) ?? throw new InvalidOperationException("The updated executable did not start.");

            if (await WaitForMarkerAsync(markerPath, updated))
            {
                TryDelete(backupPath);
                TryDelete(markerPath);
                return;
            }

            await TryStopAsync(updated);
            File.Copy(backupPath, targetPath, true);
            Process.Start(new ProcessStartInfo(targetPath) { UseShellExecute = true });
            MessageBox.Show("The new version did not finish starting. The previous version was restored.", "DLSS Feeder Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception exception)
        {
            TryRestore(backupPath, targetPath);
            MessageBox.Show($"The update could not be applied.\n\n{exception.Message}", "DLSS Feeder Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static bool IsSafeTarget(string value, out string path)
    {
        try
        {
            path = Path.GetFullPath(value);
            return Path.IsPathFullyQualified(path) &&
                   string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase) &&
                   File.Exists(path);
        }
        catch
        {
            path = string.Empty;
            return false;
        }
    }

    private static bool TryFullPath(string value, out string path)
    {
        try
        {
            path = Path.GetFullPath(value);
            return Path.IsPathFullyQualified(path);
        }
        catch
        {
            path = string.Empty;
            return false;
        }
    }

    private static bool IsSafeMarker(string value, out string path)
    {
        try
        {
            path = Path.GetFullPath(value);
            var updateRoot = Path.GetFullPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DLSS Feeder Manager",
                "updates")) + Path.DirectorySeparatorChar;
            return path.StartsWith(updateRoot, StringComparison.OrdinalIgnoreCase) && path.EndsWith(".ready", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            path = string.Empty;
            return false;
        }
    }

    private static async Task WaitForExitAsync(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (ArgumentException)
        {
        }
    }

    private static async Task<bool> WaitForMarkerAsync(string markerPath, Process process)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            if (File.Exists(markerPath))
                return true;
            if (process.HasExited)
                return false;
            await Task.Delay(500);
        }
        return false;
    }

    private static async Task TryStopAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await process.WaitForExitAsync(timeout.Token);
            }
        }
        catch
        {
        }
    }

    private static void TryRestore(string backupPath, string targetPath)
    {
        try
        {
            if (File.Exists(backupPath))
                File.Copy(backupPath, targetPath, true);
        }
        catch
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}
