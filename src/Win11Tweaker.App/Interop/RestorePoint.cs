using System.Runtime.InteropServices;

namespace Win11Tweaker.App.Interop;

public static class RestorePoint
{
    static bool doneThisSession;

    public static bool TryCreateOnce(string description)
    {
        if (doneThisSession || !Elevation.IsAdmin)
            return false;

        doneThisSession = true;

        try
        {
            var info = new RESTOREPOINTINFO
            {
                dwEventType = BEGIN_SYSTEM_CHANGE,
                dwRestorePtType = MODIFY_SETTINGS,
                llSequenceNumber = 0,
                szDescription = description
            };

            if (!SRSetRestorePointW(ref info, out var status))
                return false;

            info.dwEventType = END_SYSTEM_CHANGE;
            info.llSequenceNumber = status.llSequenceNumber;
            SRSetRestorePointW(ref info, out _);

            return status.nStatus == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    const int BEGIN_SYSTEM_CHANGE = 100;
    const int END_SYSTEM_CHANGE = 101;
    const int MODIFY_SETTINGS = 12;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct RESTOREPOINTINFO
    {
        public int dwEventType;
        public int dwRestorePtType;
        public long llSequenceNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct STATEMGRSTATUS
    {
        public int nStatus;
        public long llSequenceNumber;
    }

    [DllImport("srclient.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SRSetRestorePointW(ref RESTOREPOINTINFO pRestorePtSpec, out STATEMGRSTATUS pSMgrStatus);
}
