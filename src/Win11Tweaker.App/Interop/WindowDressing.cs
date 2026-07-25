using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Win11Tweaker.App.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct Edges
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Spot
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MonitorInfo
{
    public int Size;
    public Edges Monitor;
    public Edges Work;
    public uint Flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MinMaxInfo
{
    public Spot Reserved;
    public Spot MaxSize;
    public Spot MaxPosition;
    public Spot MinTrackSize;
    public Spot MaxTrackSize;
}

public static partial class WindowDressing
{
    const int UseImmersiveDarkMode = 20;
    const int BorderColor = 34;
    const int CornerPreference = 33;
    const int RoundedCorners = 2;

    public static void ApplyDarkFrame(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return;

        var dark = 1;
        DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref dark, sizeof(int));

        var corners = RoundedCorners;
        DwmSetWindowAttribute(handle, CornerPreference, ref corners, sizeof(int));

        var frame = 0x00342C2C;
        DwmSetWindowAttribute(handle, BorderColor, ref frame, sizeof(int));
    }

    public static void ClampMaximizeToWorkArea(Window window)
    {
        if (PresentationSource.FromVisual(window) is HwndSource source)
            source.AddHook(OnWindowMessage);
    }

    static IntPtr OnWindowMessage(IntPtr handle, int message, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        const int GetMinMaxInfo = 0x0024;
        const int NearestMonitor = 2;

        if (message != GetMinMaxInfo)
            return IntPtr.Zero;

        var monitor = MonitorFromWindow(handle, NearestMonitor);
        if (monitor == IntPtr.Zero)
            return IntPtr.Zero;

        var screen = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfoW(monitor, ref screen))
            return IntPtr.Zero;

        var bounds = Marshal.PtrToStructure<MinMaxInfo>(lparam);
        bounds.MaxPosition.X = screen.Work.Left - screen.Monitor.Left;
        bounds.MaxPosition.Y = screen.Work.Top - screen.Monitor.Top;
        bounds.MaxSize.X = screen.Work.Right - screen.Work.Left;
        bounds.MaxSize.Y = screen.Work.Bottom - screen.Work.Top;
        Marshal.StructureToPtr(bounds, lparam, fDeleteOld: true);

        handled = true;
        return IntPtr.Zero;
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    [LibraryImport("user32.dll")]
    private static partial IntPtr MonitorFromWindow(IntPtr window, int flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfoW(IntPtr monitor, ref MonitorInfo info);
}
