using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Win11Tweaker.App.Interop;

public static partial class ShellRefresh
{
    const int AssocChanged = 0x08000000;
    const uint IdList = 0x0000;

    public static void Nudge() => SHChangeNotify(AssocChanged, IdList, IntPtr.Zero, IntPtr.Zero);

    public static void RestartExplorer()
    {
        foreach (var shell in Process.GetProcessesByName("explorer"))
        {
            try
            {
                shell.Kill();
                shell.WaitForExit(4000);
            }
            catch (Exception)
            {
            }
            finally
            {
                shell.Dispose();
            }
        }

        Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true })?.Dispose();
    }

    [LibraryImport("shell32.dll")]
    private static partial void SHChangeNotify(int eventId, uint flags, IntPtr first, IntPtr second);
}
