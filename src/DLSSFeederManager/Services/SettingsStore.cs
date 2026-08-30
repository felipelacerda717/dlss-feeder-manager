using System.Text.Json;
using DLSSFeederManager.Models;

namespace DLSSFeederManager.Services;

public sealed class SettingsStore
{
    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DLSS Feeder Manager");

    private string SettingsPath => Path.Combine(_directory, "settings.json");

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        await using var stream = File.OpenRead(SettingsPath);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream) ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(_directory);
        var temporaryPath = SettingsPath + ".tmp";

        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        File.Move(temporaryPath, SettingsPath, true);
    }
}
