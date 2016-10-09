/****************************** Module Header ******************************\
 Module Name:  INTERNET_PER_CONN_OPTION.cs
 Project:      CSWebBrowserWithProxy
 Copyright (c) Microsoft Corporation.
 
 This file defines the struct INTERNET_PER_CONN_OPTION and constants used by it.
 The struct INTERNET_PER_CONN_OPTION contains the value of an option that to be 
 set to internet settings.
 Visit http://msdn.microsoft.com/en-us/library/aa385145(VS.85).aspx to get the 
 detailed description.
 
 This source is subject to the Microsoft Public License.
 See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
 All other rights reserved.
 
 THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
 EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
 WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\***************************************************************************/

using System;
using System.Runtime.InteropServices;

namespace Shadowsocks.Util.SystemProxy
{
    /// <summary>
    /// Constants used in INTERNET_PER_CONN_OPTION_OptionUnion struct.
    /// </summary>
    public enum INTERNET_PER_CONN_OptionEnum
    {
        INTERNET_PER_CONN_FLAGS = 1,
        INTERNET_PER_CONN_PROXY_SERVER = 2,
        INTERNET_PER_CONN_PROXY_BYPASS = 3,
        INTERNET_PER_CONN_AUTOCONFIG_URL = 4,
        INTERNET_PER_CONN_AUTODISCOVERY_FLAGS = 5,
        INTERNET_PER_CONN_AUTOCONFIG_SECONDARY_URL = 6,
        INTERNET_PER_CONN_AUTOCONFIG_RELOAD_DELAY_MINS = 7,
        INTERNET_PER_CONN_AUTOCONFIG_LAST_DETECT_TIME = 8,
        INTERNET_PER_CONN_AUTOCONFIG_LAST_DETECT_URL = 9,
        INTERNET_PER_CONN_FLAGS_UI = 10
    }

    /// <summary>
    /// Constants used in INTERNET_PER_CONN_OPTON struct.
    /// </summary>
    [Flags]
    public enum INTERNET_OPTION_PER_CONN_FLAGS
    {
        PROXY_TYPE_DIRECT = 0x00000001,   // direct to net
        PROXY_TYPE_PROXY = 0x00000002,   // via named proxy
        PROXY_TYPE_AUTO_PROXY_URL = 0x00000004,   // autoproxy URL
        PROXY_TYPE_AUTO_DETECT = 0x00000008   // use autoproxy detection
    }

    /// <summary>
    /// Constants used in INTERNET_PER_CONN_OPTON struct.
    /// Windows 7 and later:  
    /// Clients that support Internet Explorer 8 should query the connection type using INTERNET_PER_CONN_FLAGS_UI.
    /// If this query fails, then the system is running a previous version of Internet Explorer and the client should
    /// query again with INTERNET_PER_CONN_FLAGS.
    /// Restore the connection type using INTERNET_PER_CONN_FLAGS regardless of the version of Internet Explorer.
    /// XXX: If fails, notify user to upgrade Internet Explorer
    /// </summary>
    [Flags]
    public enum INTERNET_OPTION_PER_CONN_FLAGS_UI
    {
        PROXY_TYPE_DIRECT = 0x00000001,   // direct to net
        PROXY_TYPE_PROXY = 0x00000002,   // via named proxy
        PROXY_TYPE_AUTO_PROXY_URL = 0x00000004,   // autoproxy URL
        PROXY_TYPE_AUTO_DETECT = 0x00000008   // use autoproxy detection
    }

    /// <summary>
    /// Used in INTERNET_PER_CONN_OPTION.
    /// When create a instance of OptionUnion, only one filed will be used.
    /// The StructLayout and FieldOffset attributes could help to decrease the struct size.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct INTERNET_PER_CONN_OPTION_OptionUnion : IDisposable
    {
        // A value in INTERNET_OPTION_PER_CONN_FLAGS.
        [FieldOffset(0)]
        public int dwValue;
        [FieldOffset(0)]
        public System.IntPtr pszValue;
        [FieldOffset(0)]
        public System.Runtime.InteropServices.ComTypes.FILETIME ftValue;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (pszValue != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pszValue);
                    pszValue = IntPtr.Zero;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNET_PER_CONN_OPTION
    {
        // A value in INTERNET_PER_CONN_OptionEnum.
        public int dwOption;
        public INTERNET_PER_CONN_OPTION_OptionUnion Value;
    }
}
