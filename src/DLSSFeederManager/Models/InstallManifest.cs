namespace DLSSFeederManager.Models;

public sealed class InstallManifest
{
    public string GameExecutable { get; set; } = "";
    public string ProfileId { get; set; } = "generic";
    public string FeederRelease { get; set; } = "";
    public DateTimeOffset InstalledAt { get; set; }
    public List<InstalledFile> Files { get; set; } = [];
}

public sealed class InstalledFile
{
    public string RelativePath { get; set; } = "";
    public bool HadOriginal { get; set; }
    public string? BackupRelativePath { get; set; }
    public string? BackupSha256 { get; set; }
    public string? InstalledSha256 { get; set; }
}
