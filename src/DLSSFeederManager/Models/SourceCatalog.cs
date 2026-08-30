namespace DLSSFeederManager.Models;

public sealed class SourceCatalog
{
    public string DefaultFeederRelease { get; set; } = "";
    public List<FeederRelease> FeederReleases { get; set; } = [];
}

public sealed class FeederRelease
{
    public string Version { get; set; } = "";
    public List<DownloadFile> Files { get; set; } = [];
}

public sealed class DownloadFile
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string Destination { get; set; } = "";
}
