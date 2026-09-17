using System.Reflection;

namespace Rumours.App;

public static class AppInfo
{
    public static string Version { get; } =
        (Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "")
        .Split('+')[0];

    public static string Title => $"PoE2 Rumours {Version}";
}
