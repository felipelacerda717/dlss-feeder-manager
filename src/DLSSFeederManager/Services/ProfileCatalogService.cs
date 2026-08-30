using DLSSFeederManager.Models;

namespace DLSSFeederManager.Services;

public sealed class ProfileCatalogService
{
    private readonly ProfileDocument _document = EmbeddedJson.Load<ProfileDocument>("games.json");

    public GameProfile? FindByExecutable(string executablePath)
    {
        var name = Path.GetFileName(executablePath);
        return _document.Profiles.FirstOrDefault(profile =>
            profile.Executables.Any(executable =>
                string.Equals(executable, name, StringComparison.OrdinalIgnoreCase)));
    }
}
