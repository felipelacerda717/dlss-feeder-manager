namespace DLSSFeederManager.Services;

public sealed record UpdateRelease(
    string Version,
    string Notes,
    string DownloadUrl,
    string ChecksumUrl);
