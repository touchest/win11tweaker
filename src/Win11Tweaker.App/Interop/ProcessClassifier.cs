using System.Diagnostics;

namespace Win11Tweaker.App.Interop;

public sealed record ProcessMeta(string Category, string Publisher, string Path, bool Candidate, string Reason, bool Killable);

public static class ProcessClassifier
{
    static readonly HashSet<string> Critical = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Idle", "Registry", "Secure System", "Memory Compression", "MemCompression",
        "smss", "csrss", "wininit", "winlogon", "services", "lsass", "lsaiso", "fontdrvhost",
        "dwm", "sihost", "ctfmon", "explorer", "taskhostw", "RuntimeBroker", "svchost", "spoolsv",
        "SearchHost", "SearchIndexer", "SearchApp", "ShellExperienceHost", "StartMenuExperienceHost",
        "TextInputHost", "ApplicationFrameHost", "dllhost", "conhost", "WmiPrvSE", "audiodg",
        "LogonUI", "WUDFHost", "dwm", "MsMpEng", "NisSrv", "SecurityHealthService", "SecurityHealthSystray",
        "Win11Tweaker"
    };

    static readonly string WinDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\');

    public static ProcessMeta Classify(Process process, TrustList trust)
    {
        var name = process.ProcessName;
        var hasWindow = process.MainWindowHandle != IntPtr.Zero;

        string? path = null;
        try { path = process.MainModule?.FileName; }
        catch (Exception) {  }

        var underWindows = path is not null && path.StartsWith(WinDir, StringComparison.OrdinalIgnoreCase);
        if (Critical.Contains(name) || underWindows || path is null)
            return new ProcessMeta("Система", "Windows", path ?? "нет доступа", false, string.Empty, false);

        var (signed, signer) = SignatureVerifier.Check(path);

        if (signed && signer is not null && signer.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
            return new ProcessMeta("Windows", signer, path, false, string.Empty, false);

        if (trust.IsException(name))
            return new ProcessMeta("Доверенный", signer ?? "в исключениях", path, false, string.Empty, true);

        if (signed)
            return new ProcessMeta("Подписан", signer ?? "подпись есть", path, false, string.Empty, true);

        if (hasWindow)
            return new ProcessMeta("Приложение", "без подписи", path, false, string.Empty, true);

        return new ProcessMeta("Не подписан", "без подписи", path, true, "фоновый, без подписи", true);
    }
}
