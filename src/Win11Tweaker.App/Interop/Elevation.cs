using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace Win11Tweaker.App.Interop;

public static class Elevation
{
    public static bool IsAdmin { get; } = Check();

    static bool Check()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static bool RelaunchAsAdmin(string? argument = null)
    {
        var exe = Environment.ProcessPath;
        if (exe is null)
            return false;

        var start = new ProcessStartInfo(exe)
        {
            UseShellExecute = true,
            Verb = "runas"
        };
        if (argument is not null)
            start.ArgumentList.Add(argument);

        try
        {
            Process.Start(start);
        }
        catch (Exception)
        {
            return false;
        }

        Application.Current.Shutdown();
        return true;
    }
}
