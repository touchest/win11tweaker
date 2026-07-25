using System.Reflection;

namespace Win11Tweaker.App.Shell;

public static class AppInfo
{
    public static string Version { get; } = Read();

    public static string Full { get; } = "Win11 Tweaker " + Version;

    static string Read()
    {
        var asm = Assembly.GetExecutingAssembly();

        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var plus = info.IndexOf('+');
            return plus > 0 ? info[..plus] : info;
        }

        var v = asm.GetName().Version;
        return v is null ? "0.0.0" : $"{v.Major}.{v.Minor}.{v.Build}";
    }
}
