using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Shadowsocks.WPF.Services.SystemProxy;

public enum RasFieldSizeConst
{
    MaxEntryName = 256,
    MaxPath = 260,
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct RasEntryName
{
    public int dwSize;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Ras.MaxEntryName + 1)]
    public string szEntryName;

    public int dwFlags;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Ras.MaxPath + 1)]
    public string szPhonebookPath;
}

public class Ras
{
    public const int MaxEntryName = 256;
    public const int MaxPath = 260;

    private const int E_SUCCESS = 0;
    private const int RAS_BASE = 600;
    private const int E_BUFFER_TOO_SMALL = 603;

    [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
    private static extern uint RasEnumEntries(
        // reserved, must be NULL
        string reserved,
        // pointer to full path and file name of phone-book file
        string lpszPhonebook,
        // buffer to receive phone-book entries
        [In, Out] RasEntryName[]? lprasentryname,
        // size in bytes of buffer
        ref int lpcb,
        // number of entries written to buffer
        out int lpcEntries
    );

    public static string[] GetAllConnections()
    {
        var lpNames = 0;
        var entryNameSize = 0;
        var lpSize = 0;
        uint retval = E_SUCCESS;
        var names = Array.Empty<RasEntryName>();

        entryNameSize = Marshal.SizeOf(typeof(RasEntryName));

        // Windows Vista or later:  To determine the required buffer size, call RasEnumEntries
        // with lprasentryname set to NULL. The variable pointed to by lpcb should be set to zero.
        // The function will return the required buffer size in lpcb and an error code of ERROR_BUFFER_TOO_SMALL.
        retval = RasEnumEntries("", "", null, ref lpSize, out lpNames);
        if (retval == E_BUFFER_TOO_SMALL)
        {
            names = new RasEntryName[lpNames];
            for (var i = 0; i < names.Length; i++)
            {
                names[i].dwSize = entryNameSize;
            }

            retval = RasEnumEntries("", "", names, ref lpSize, out lpNames);
        }

        if (retval == E_SUCCESS)
        {
            if (lpNames == 0)
            {
                // no entries found.
                return Array.Empty<string>();
            }
            return names.Select(n => n.szEntryName).ToArray();
        }

        throw new Exception();
    }
}