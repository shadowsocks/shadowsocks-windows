using System.Runtime.InteropServices;

namespace Shadowsocks.Util.SystemProxy
{
    internal static class RemoteAccessService
    {
        private enum RasFieldSizeConstants
        {
            #region original header

            //#if (WINVER >= 0x400)
            //#define RAS_MaxEntryName      256
            //#define RAS_MaxDeviceName     128
            //#define RAS_MaxCallbackNumber RAS_MaxPhoneNumber
            //#else
            //#define RAS_MaxEntryName      20
            //#define RAS_MaxDeviceName     32
            //#define RAS_MaxCallbackNumber 48
            //#endif

            #endregion

            RAS_MaxEntryName = 256,
            RAS_MaxPath = 260
        }

        private const int ERROR_SUCCESS = 0;
        private const int RASBASE = 600;
        private const int ERROR_BUFFER_TOO_SMALL = RASBASE + 3;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct RasEntryName
        {
            #region original header

            //#define RASENTRYNAMEW struct tagRASENTRYNAMEW
            //RASENTRYNAMEW
            //{
            //    DWORD dwSize;
            //    WCHAR szEntryName[RAS_MaxEntryName + 1];
            //
            //#if (WINVER >= 0x500)
            //    //
            //    // If this flag is REN_AllUsers then its a
            //    // system phonebook.
            //    //
            //    DWORD dwFlags;
            //    WCHAR szPhonebookPath[MAX_PATH + 1];
            //#endif
            //};
            //
            //#define RASENTRYNAMEA struct tagRASENTRYNAMEA
            //RASENTRYNAMEA
            //{
            //    DWORD dwSize;
            //    CHAR szEntryName[RAS_MaxEntryName + 1];
            //
            //#if (WINVER >= 0x500)
            //    DWORD dwFlags;
            //    CHAR  szPhonebookPath[MAX_PATH + 1];
            //#endif
            //};

            #endregion

            public int dwSize;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=(int)RasFieldSizeConstants.RAS_MaxEntryName + 1)]
            public string szEntryName;

            public int dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=(int)RasFieldSizeConstants.RAS_MaxPath + 1)]
            public string szPhonebookPath;
        }

        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern uint RasEnumEntries(
            // reserved, must be NULL
            string reserved,
            // pointer to full path and file name of phone-book file
            string lpszPhonebook,
            // buffer to receive phone-book entries
            [In, Out] RasEntryName[] lprasentryname,
            // size in bytes of buffer
            ref int lpcb,
            // number of entries written to buffer
            out int lpcEntries
        );

        /// <summary>
        /// Get all entries from RAS
        /// </summary>
        /// <param name="allConns"></param>
        /// <returns>
        /// 0: success with entries
        /// 1: success but no entries found
        /// 2: failed
        /// </returns>
        public static uint GetAllConns(ref string[] allConns)
        {
            int lpNames = 0;
            int entryNameSize = 0;
            int lpSize = 0;
            uint retval = ERROR_SUCCESS;
            RasEntryName[] names = null;

            entryNameSize = Marshal.SizeOf(typeof(RasEntryName));

            // Windows Vista or later:  To determine the required buffer size, call RasEnumEntries
            // with lprasentryname set to NULL. The variable pointed to by lpcb should be set to zero.
            // The function will return the required buffer size in lpcb and an error code of ERROR_BUFFER_TOO_SMALL.
            retval = RasEnumEntries(null, null, null, ref lpSize, out lpNames);

            if (retval == ERROR_BUFFER_TOO_SMALL)
            {
                names = new RasEntryName[lpNames];
                for (int i = 0; i < names.Length; i++)
                {
                    names[i].dwSize = entryNameSize;
                }

                retval = RasEnumEntries(null, null, names, ref lpSize, out lpNames);
            }

            if (retval == ERROR_SUCCESS)
            {
                if (lpNames == 0)
                {
                    // no entries found.
                    return 1;
                }

                allConns = new string[names.Length];

                for (int i = 0; i < names.Length; i++)
                {
                    allConns[i] = names[i].szEntryName;
                }
                return 0;
            }
            else
            {
                return 2;
            }
        }
    }
}