/****************************** Module Header ******************************\
 Module Name:  INTERNET_PER_CONN_OPTION_LIST.cs
 Project:      CSWebBrowserWithProxy
 Copyright (c) Microsoft Corporation.
 
 The struct INTERNET_PER_CONN_OPTION contains a list of options that to be 
 set to internet connection.
 Visit http://msdn.microsoft.com/en-us/library/aa385146(VS.85).aspx to get the 
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
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct INTERNET_PER_CONN_OPTION_LIST : IDisposable
    {
        public int Size;

        // The connection to be set. NULL means LAN.
        public System.IntPtr Connection;

        public int OptionCount;
        public int OptionError;

        // List of INTERNET_PER_CONN_OPTIONs.
        public System.IntPtr pOptions;

        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        private void Dispose( bool disposing )
        {
            if ( disposing )
            {
                if ( Connection != IntPtr.Zero )
                {
                    Marshal.FreeHGlobal( Connection );
                    Connection = IntPtr.Zero;
                }

                if ( pOptions != IntPtr.Zero )
                {
                    Marshal.FreeHGlobal( pOptions );
                    pOptions = IntPtr.Zero;
                }
            }
        }
    }
}
