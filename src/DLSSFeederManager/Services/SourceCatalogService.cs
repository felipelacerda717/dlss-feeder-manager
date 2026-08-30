using DLSSFeederManager.Models;

namespace DLSSFeederManager.Services;

public sealed class SourceCatalogService
{
    private readonly SourceCatalog _catalog = EmbeddedJson.Load<SourceCatalog>("sources.json");

    public FeederRelease GetFeederRelease(GameProfile? profile)
    {
        var version = string.IsNullOrWhiteSpace(profile?.FeederRelease)
            ? _catalog.DefaultFeederRelease
            : profile.FeederRelease;

        return _catalog.FeederReleases.Single(release =>
            string.Equals(release.Version, version, StringComparison.OrdinalIgnoreCase));
    }
}
