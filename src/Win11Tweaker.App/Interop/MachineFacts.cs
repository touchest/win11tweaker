using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Win11Tweaker.App.Interop;

public static partial class MachineFacts
{
    const string CurrentVersion = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

    public static string Edition()
    {
        using var key = Registry.LocalMachine.OpenSubKey(CurrentVersion);

        var edition = key?.GetValue("EditionID") as string ?? "неизвестно";
        var family = BuildNumber() >= 22000 ? "Windows 11" : "Windows 10";
        return family + " " + edition;
    }

    public static string BuildLabel()
    {
        using var key = Registry.LocalMachine.OpenSubKey(CurrentVersion);
        var release = key?.GetValue("DisplayVersion") as string ?? key?.GetValue("ReleaseId") as string;
        var revision = key?.GetValue("UBR") as int? ?? 0;
        var stamp = BuildNumber() + "." + revision;
        return release is null ? stamp : release + "  (" + stamp + ")";
    }

    public static int BuildNumber()
    {
        using var key = Registry.LocalMachine.OpenSubKey(CurrentVersion);
        return int.TryParse(key?.GetValue("CurrentBuild") as string, out var build) ? build : 0;
    }

    public static bool TamperProtectionOn()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Features");
            return key?.GetValue("TamperProtection") as int? == 5;
        }
        catch (System.Security.SecurityException)
        {
            return true;
        }
    }

    public static bool DefenderRealtimeOn()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection");

            return key?.GetValue("DisableRealtimeMonitoring") as int? is not 1;
        }
        catch (System.Security.SecurityException)
        {
            return true;
        }
    }

    public static string Graphics()
    {
        const string classRoot = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

        for (var index = 0; index < 8; index++)
        {
            using var adapter = Registry.LocalMachine.OpenSubKey(classRoot + "\\" + index.ToString("0000"));
            if (adapter?.GetValue("DriverDesc") is not string name)
                continue;

            if (name.Contains("Basic Display", StringComparison.OrdinalIgnoreCase))
                continue;

            if (adapter.GetValue("HardwareInformation.qwMemorySize") is long bytes && bytes > 0)
            {
                var gb = bytes / 1024d / 1024d / 1024d;
                return name + "  /  " + gb.ToString("0.#") + " ГБ";
            }

            return name;
        }

        return "неизвестно";
    }

    public static string Processor()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        var name = (key?.GetValue("ProcessorNameString") as string)?.Trim() ?? "неизвестно";
        return name + "  /  " + Environment.ProcessorCount + " потоков";
    }

    public static string Memory()
    {
        var (total, used, load) = MemorySnapshot();
        if (total == 0)
            return "неизвестно";

        return total.ToString("0.#") + " ГБ  /  занято " + used.ToString("0.#") + " ГБ (" + load + "%)";
    }

    public static (double TotalGb, double UsedGb, uint LoadPercent) MemorySnapshot()
    {
        var probe = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        if (!GlobalMemoryStatusEx(ref probe))
            return (0, 0, 0);

        const double gb = 1024d * 1024d * 1024d;
        var total = probe.TotalPhys / gb;
        return (total, total - probe.AvailPhys / gb, probe.MemoryLoad);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalMemoryStatusEx(ref MemoryStatus probe);
}
