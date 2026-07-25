using System.Runtime.InteropServices;

namespace Win11Tweaker.App.Interop;

public static partial class SystemLoad
{
    [StructLayout(LayoutKind.Sequential)]
    struct FileTime
    {
        public uint Low;
        public uint High;

        public readonly ulong Ticks => ((ulong)High << 32) | Low;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);

    static ulong prevIdle, prevBusy;

    public static double CpuPercent()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
            return 0;

        var idleTicks = idle.Ticks;
        var totalTicks = kernel.Ticks + user.Ticks;
        var busyTicks = totalTicks - idleTicks;

        var dIdle = idleTicks - prevIdle;
        var dBusy = busyTicks - prevBusy;
        prevIdle = idleTicks;
        prevBusy = busyTicks;

        var span = dIdle + dBusy;
        return span == 0 ? 0 : Math.Clamp(dBusy * 100d / span, 0, 100);
    }
}
