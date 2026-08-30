using System.Reflection;

namespace DLSSFeederManager.Services;

public static class AppVersion
{
    public static string Current { get; } =
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+', 2)[0] ?? "0.0.0";
}
