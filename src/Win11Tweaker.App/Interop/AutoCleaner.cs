using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Win11Tweaker.App.Interop;

public readonly record struct SweepResult(int Files, long Bytes)
{
    public string Freed => Format(Bytes);

    public static string Format(long bytes) => bytes >= 1024L * 1024 * 1024
        ? (bytes / 1024d / 1024 / 1024).ToString("0.#") + " ГБ"
        : (bytes / 1024d / 1024).ToString("0") + " МБ";
}

public static partial class AutoCleaner
{
    static readonly TimeSpan Stale = TimeSpan.FromHours(24);

    public static SweepResult Sweep(bool temp, bool recycle)
    {
        var files = 0;
        var bytes = 0L;

        if (temp)
            foreach (var dir in TempDirs())
                SweepStale(dir, ref files, ref bytes);

        if (recycle)
            EmptyRecycleBin();

        return new SweepResult(files, bytes);
    }

    static IEnumerable<string> TempDirs()
    {
        yield return Path.GetTempPath();

        var winTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
        if (Directory.Exists(winTemp))
            yield return winTemp;
    }

    static void SweepStale(string dir, ref int files, ref long bytes)
    {
        var root = SafeDir(dir);
        if (root is null)
            return;

        var cutoff = DateTime.Now - Stale;

        foreach (var file in Enumerate(root))
        {
            if (file.LastWriteTime > cutoff)
                continue;

            long size;
            try { size = file.Length; }
            catch (Exception) { continue; }

            try { file.Delete(); files++; bytes += size; }
            catch (Exception) {  }
        }
    }

    public static long DirBytes(params string[] dirs)
    {
        var sum = 0L;
        foreach (var dir in dirs)
        {
            var root = SafeDir(dir);
            if (root is null)
                continue;
            foreach (var file in Enumerate(root))
                try { sum += file.Length; }
                catch (Exception) { }
        }
        return sum;
    }

    public static SweepResult PurgeDirs(params string[] dirs)
    {
        var files = 0;
        var bytes = 0L;
        foreach (var dir in dirs)
        {
            var root = SafeDir(dir);
            if (root is null)
                continue;
            foreach (var file in Enumerate(root))
            {
                long size;
                try { size = file.Length; }
                catch (Exception) { continue; }

                try { file.Delete(); files++; bytes += size; }
                catch (Exception) { }
            }
        }
        return new SweepResult(files, bytes);
    }

    public static long GlobBytes(string dir, string pattern)
    {
        var root = SafeDir(dir);
        if (root is null)
            return 0;
        var sum = 0L;
        try
        {
            foreach (var file in root.GetFiles(pattern, SearchOption.TopDirectoryOnly))
                try { sum += file.Length; }
                catch (Exception) { }
        }
        catch (Exception) { }
        return sum;
    }

    public static SweepResult PurgeGlob(string dir, string pattern)
    {
        var root = SafeDir(dir);
        if (root is null)
            return default;
        FileInfo[] hits;
        try { hits = root.GetFiles(pattern, SearchOption.TopDirectoryOnly); }
        catch (Exception) { return default; }

        var files = 0;
        var bytes = 0L;
        foreach (var file in hits)
        {
            long size;
            try { size = file.Length; }
            catch (Exception) { continue; }

            try { file.Delete(); files++; bytes += size; }
            catch (Exception) { }
        }
        return new SweepResult(files, bytes);
    }

    public static long FileBytes(string path)
    {
        try { return File.Exists(path) ? new FileInfo(path).Length : 0; }
        catch (Exception) { return 0; }
    }

    public static SweepResult DeleteFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return default;
            var size = new FileInfo(path).Length;
            File.Delete(path);
            return new SweepResult(1, size);
        }
        catch (Exception) { return default; }
    }

    public static long RecycleBinBytes()
    {
        var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
        return SHQueryRecycleBin(null, ref info) == 0 ? info.i64Size : 0;
    }

    public static SweepResult PurgeRecycleBin()
    {
        var size = RecycleBinBytes();
        EmptyRecycleBin();
        return new SweepResult(0, size);
    }

    public static SweepResult DisableHibernation()
    {
        var hiberfil = Path.Combine(SystemDriveRoot(), "hiberfil.sys");
        var size = FileBytes(hiberfil);
        Run("powercfg.exe", "/hibernate off", 30_000);
        return new SweepResult(0, size);
    }

    public static SweepResult ComponentCleanup()
    {
        Run("dism.exe", "/online /cleanup-image /startcomponentcleanup", 600_000);
        return default;
    }

    public static SweepResult ComponentDeepCleanup()
    {
        Run("dism.exe", "/online /cleanup-image /startcomponentcleanup /resetbase", 900_000);
        return default;
    }

    public static bool CompactOsOn()
    {
        var output = Capture("compact.exe", "/compactos:query");

        var lower = output.ToLowerInvariant();
        if (lower.Contains("not") || lower.Contains("не сжат") || lower.Contains("не в сжат"))
            return false;
        return lower.Contains("compact") || lower.Contains("сжат");
    }

    public static bool SetCompactOs(bool on)
        => Run("compact.exe", on ? "/compactos:always" : "/compactos:never", 1_200_000);

    static string Capture(string exe, string args)
    {
        try
        {
            var p = Process.Start(new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });
            if (p is null)
                return string.Empty;
            var text = p.StandardOutput.ReadToEnd();
            p.WaitForExit(30_000);
            return text;
        }
        catch (Exception) { return string.Empty; }
    }

    public static string SystemDriveRoot() =>
        Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";

    static bool Run(string exe, string args, int timeoutMs)
    {
        try
        {
            var p = Process.Start(new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (p is null)
                return false;
            p.WaitForExit(timeoutMs);
            return p.HasExited && p.ExitCode == 0;
        }
        catch (Exception) { return false; }
    }

    static DirectoryInfo? SafeDir(string dir)
    {
        try
        {
            var d = new DirectoryInfo(dir);
            return d.Exists ? d : null;
        }
        catch (Exception) { return null; }
    }

    static IEnumerable<FileInfo> Enumerate(DirectoryInfo root)
    {
        var stack = new Stack<DirectoryInfo>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var dir = stack.Pop();

            FileInfo[] here;
            try { here = dir.GetFiles(); }
            catch (Exception) { continue; }

            foreach (var file in here)
                yield return file;

            DirectoryInfo[] subs;
            try { subs = dir.GetDirectories(); }
            catch (Exception) { continue; }

            foreach (var sub in subs)
                stack.Push(sub);
        }
    }

    static void EmptyRecycleBin()
    {
        const uint noConfirm = 0x1;
        const uint noProgress = 0x2;
        const uint noSound = 0x4;
        SHEmptyRecycleBin(IntPtr.Zero, null, noConfirm | noProgress | noSound);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [LibraryImport("shell32.dll", EntryPoint = "SHEmptyRecycleBinW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHEmptyRecycleBin(IntPtr owner, string? root, uint flags);

    [LibraryImport("shell32.dll", EntryPoint = "SHQueryRecycleBinW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHQueryRecycleBin(string? root, ref SHQUERYRBINFO info);
}
