using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace Win11Tweaker.App.Interop;

public static class TrustedInstaller
{
    public static void RunAs(Action work)
    {
        SafeAccessTokenHandle? handle = null;

        try
        {
            EnablePrivilege("SeDebugPrivilege");
            EnablePrivilege("SeImpersonatePrivilege");
            handle = new SafeAccessTokenHandle(AcquireTrustedInstallerToken());
        }
        catch (InvalidOperationException)
        {
            handle = null;
        }

        if (handle is null)
        {
            work();
        }
        else
        {
            using (handle)
                WindowsIdentity.RunImpersonated(handle, work);
        }
    }

    static void EnablePrivilege(string name)
    {
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var hToken))
            throw Fail("OpenProcessToken");

        try
        {
            if (!LookupPrivilegeValue(null, name, out var luid))
                throw Fail("LookupPrivilegeValue " + name);

            var privileges = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED
            };

            if (!AdjustTokenPrivileges(hToken, false, ref privileges, 0, IntPtr.Zero, IntPtr.Zero))
                throw Fail("AdjustTokenPrivileges " + name);

            if (Marshal.GetLastWin32Error() == ERROR_NOT_ALL_ASSIGNED)
                throw new InvalidOperationException(
                    $"Привилегия {name} недоступна. Нужен запуск от администратора.");
        }
        finally { CloseHandle(hToken); }
    }

    static uint StartAndGetPid()
    {
        var scm = OpenSCManager(null, null, SC_MANAGER_CONNECT);
        if (scm == IntPtr.Zero) throw Fail("OpenSCManager");

        try
        {
            var svc = OpenService(scm, "TrustedInstaller", SERVICE_START | SERVICE_QUERY_STATUS);
            if (svc == IntPtr.Zero) throw Fail("OpenService(TrustedInstaller)");

            try
            {
                var size = Marshal.SizeOf<SERVICE_STATUS_PROCESS>();

                for (var attempt = 0; attempt < 40; attempt++)
                {
                    if (!QueryServiceStatusEx(svc, SC_STATUS_PROCESS_INFO, out var status, size, out _))
                        throw Fail("QueryServiceStatusEx");

                    if (status.dwCurrentState == SERVICE_RUNNING && status.dwProcessId != 0)
                        return status.dwProcessId;

                    if (status.dwCurrentState == SERVICE_STOPPED)
                        StartService(svc, 0, null);

                    System.Threading.Thread.Sleep(150);
                }

                throw new InvalidOperationException("Служба TrustedInstaller не поднялась.");
            }
            finally { CloseServiceHandle(svc); }
        }
        finally { CloseServiceHandle(scm); }
    }

    static IntPtr AcquireTrustedInstallerToken()
    {
        var systemToken = DuplicateProcessToken(SystemProcessId(), "winlogon");
        var trustedInstallerToken = IntPtr.Zero;

        using (var systemHandle = new SafeAccessTokenHandle(systemToken))
            WindowsIdentity.RunImpersonated(systemHandle, () =>
                trustedInstallerToken = DuplicateProcessToken(StartAndGetPid(), "TrustedInstaller"));

        return trustedInstallerToken;
    }

    static uint SystemProcessId()
    {
        foreach (var process in System.Diagnostics.Process.GetProcessesByName("winlogon"))
            return (uint)process.Id;

        throw new InvalidOperationException("Не найден системный процесс winlogon.");
    }

    static IntPtr DuplicateProcessToken(uint pid, string label)
    {
        var hProc = OpenProcess(PROCESS_QUERY_INFORMATION, false, pid);
        if (hProc == IntPtr.Zero) throw Fail("OpenProcess(" + label + ")");

        try
        {
            if (!OpenProcessToken(hProc, TOKEN_DUPLICATE | TOKEN_QUERY, out var hToken))
                throw Fail("OpenProcessToken(" + label + ")");

            try
            {
                if (!DuplicateTokenEx(hToken, TOKEN_ALL_ACCESS, IntPtr.Zero,
                        SecurityImpersonation, TokenImpersonation, out var duplicate))
                    throw Fail("DuplicateTokenEx(" + label + ")");

                return duplicate;
            }
            finally { CloseHandle(hToken); }
        }
        finally { CloseHandle(hProc); }
    }

    static InvalidOperationException Fail(string call)
        => new($"{call} не удался (код {Marshal.GetLastWin32Error()}). Нужны права администратора.");

    const uint TOKEN_ADJUST_PRIVILEGES = 0x0020, TOKEN_QUERY = 0x0008, TOKEN_DUPLICATE = 0x0002;
    const uint TOKEN_ALL_ACCESS = 0xF01FF;
    const uint SE_PRIVILEGE_ENABLED = 0x0002;
    const uint SC_MANAGER_CONNECT = 0x0001;
    const uint SERVICE_START = 0x0010, SERVICE_QUERY_STATUS = 0x0004;
    const uint SC_STATUS_PROCESS_INFO = 0;
    const uint SERVICE_RUNNING = 4, SERVICE_STOPPED = 1;
    const uint PROCESS_QUERY_INFORMATION = 0x0400;
    const int ERROR_NOT_ALL_ASSIGNED = 1300;
    const int SecurityImpersonation = 2, TokenImpersonation = 2;

    [StructLayout(LayoutKind.Sequential)]
    struct LUID { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Sequential)]
    struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID Luid; public uint Attributes; }

    [StructLayout(LayoutKind.Sequential)]
    struct SERVICE_STATUS_PROCESS
    {
        public uint dwServiceType, dwCurrentState, dwControlsAccepted, dwWin32ExitCode,
            dwServiceSpecificExitCode, dwCheckPoint, dwWaitHint, dwProcessId, dwServiceFlags;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inheritHandle, uint pid);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool LookupPrivilegeValue(string? system, string name, out LUID luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll,
        ref TOKEN_PRIVILEGES newState, uint length, IntPtr previous, IntPtr returnLength);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool DuplicateTokenEx(IntPtr existing, uint access, IntPtr attributes,
        int impersonationLevel, int tokenType, out IntPtr newToken);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr OpenSCManager(string? machine, string? database, uint access);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr OpenService(IntPtr scm, string name, uint access);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool CloseServiceHandle(IntPtr handle);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool StartService(IntPtr service, uint argc, string[]? argv);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool QueryServiceStatusEx(IntPtr service, uint infoLevel,
        out SERVICE_STATUS_PROCESS status, int bufferSize, out int bytesNeeded);
}
