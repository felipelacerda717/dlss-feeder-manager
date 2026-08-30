namespace DLSSFeederManager.Models;

public sealed class GameProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public string[] Executables { get; set; } = [];
    public string FeederRelease { get; set; } = "";
}

public sealed class ProfileDocument
{
    public List<GameProfile> Profiles { get; set; } = [];
}
