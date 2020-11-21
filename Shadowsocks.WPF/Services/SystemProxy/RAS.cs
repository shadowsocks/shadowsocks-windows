using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Shadowsocks.WPF.Services.SystemProxy
{
    public enum RasFieldSizeConst
    {
        MaxEntryName = 256,
        MaxPath = 260,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct RasEntryName
    {
        public int dwSize;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS.MaxEntryName + 1)]
        public string szEntryName;

        public int dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS.MaxPath + 1)]
        public string szPhonebookPath;
    }

    public class RAS
    {
        public const int MaxEntryName = 256;
        public const int MaxPath = 260;

        const int ESuccess = 0;
        const int RasBase = 600;
        const int EBufferTooSmall = 603;

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
            int lpNames = 0;
            int entryNameSize = 0;
            int lpSize = 0;
            uint retval = ESuccess;
            RasEntryName[] names = Array.Empty<RasEntryName>();

            entryNameSize = Marshal.SizeOf(typeof(RasEntryName));

            // Windows Vista or later:  To determine the required buffer size, call RasEnumEntries
            // with lprasentryname set to NULL. The variable pointed to by lpcb should be set to zero.
            // The function will return the required buffer size in lpcb and an error code of ERROR_BUFFER_TOO_SMALL.
            retval = RasEnumEntries("", "", null, ref lpSize, out lpNames);
            if (retval == EBufferTooSmall)
            {
                names = new RasEntryName[lpNames];
                for (int i = 0; i < names.Length; i++)
                {
                    names[i].dwSize = entryNameSize;
                }

                retval = RasEnumEntries("", "", names, ref lpSize, out lpNames);
            }

            if (retval == ESuccess)
            {
                if (lpNames == 0)
                {
                    // no entries found.
                    return Array.Empty<string>();
                }
                return names.Select(n => n.szEntryName).ToArray();
            }
            else
            {
                throw new Exception();
            }
        }
    }
}
