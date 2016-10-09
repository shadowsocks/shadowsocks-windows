/****************************** Module Header ******************************\
 Module Name:  WinINet.cs
 Project:      CSWebBrowserWithProxy
 Copyright (c) Microsoft Corporation.
 
 This class is used to set the proxy. or restore to the system proxy for the
 current application
 
 This source is subject to the Microsoft Public License.
 See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
 All other rights reserved.
 
 THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
 EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
 WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\***************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Shadowsocks.Util.SystemProxy
{
    public static class WinINet
    {
        /// <summary>
        /// Set IE settings for LAN connection.
        /// </summary>
        public static void SetIEProxy(bool enable, bool global, string proxyServer, string pacURL)
        {
            List<INTERNET_PER_CONN_OPTION> _optionlist = new List<INTERNET_PER_CONN_OPTION>();
            if (enable)
            {
                if (global)
                {
                    // global proxy
                    _optionlist.Add(new INTERNET_PER_CONN_OPTION
                    {
                        dwOption = (int)INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_FLAGS_UI,
                        Value = { dwValue = (int)INTERNET_OPTION_PER_CONN_FLAGS_UI.PROXY_TYPE_PROXY }
                    });
                    _optionlist.Add(new INTERNET_PER_CONN_OPTION
                    {
                        dwOption = (int)INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_PROXY_SERVER,
                        Value = { pszValue = Marshal.StringToHGlobalAnsi(proxyServer) }
                    });
                    _optionlist.Add(new INTERNET_PER_CONN_OPTION
                    {
                        dwOption = (int)INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_PROXY_BYPASS,
                        Value = { pszValue = Marshal.StringToHGlobalAnsi("<local>") }
                    });
                }
                else
                {
                    // pac
                    _optionlist.Add(new INTERNET_PER_CONN_OPTION
                    {
                        dwOption = (int)INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_FLAGS_UI,
                        Value = { dwValue = (int)INTERNET_OPTION_PER_CONN_FLAGS_UI.PROXY_TYPE_AUTO_PROXY_URL }
                    });
                    _optionlist.Add(new INTERNET_PER_CONN_OPTION
                    {
                        dwOption = (int)INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_AUTOCONFIG_URL,
                        Value = { pszValue = Marshal.StringToHGlobalAnsi(pacURL) }
                    });
                }
            }
            else
            {
                // direct
                _optionlist.Add(new INTERNET_PER_CONN_OPTION
                {
                    dwOption = (int)INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_FLAGS_UI,
                    Value = { dwValue = (int)(INTERNET_OPTION_PER_CONN_FLAGS_UI.PROXY_TYPE_AUTO_DETECT
                                            | INTERNET_OPTION_PER_CONN_FLAGS_UI.PROXY_TYPE_DIRECT) }
                });
            }

            // Get total length of INTERNET_PER_CONN_OPTIONs
            var len = _optionlist.Sum(each => Marshal.SizeOf(each));

            // Allocate a block of memory of the options.
            IntPtr buffer = Marshal.AllocCoTaskMem(len);

            IntPtr current = buffer;

            // Marshal data from a managed object to an unmanaged block of memory.
            foreach (INTERNET_PER_CONN_OPTION eachOption in _optionlist)
            {
                Marshal.StructureToPtr(eachOption, current, false);
                current = (IntPtr)((int)current + Marshal.SizeOf(eachOption));
            }

            // Initialize a INTERNET_PER_CONN_OPTION_LIST instance.
            INTERNET_PER_CONN_OPTION_LIST optionList = new INTERNET_PER_CONN_OPTION_LIST();

            // Point to the allocated memory.
            optionList.pOptions = buffer;

            // Return the unmanaged size of an object in bytes.
            optionList.Size = Marshal.SizeOf(optionList);

            // IntPtr.Zero means LAN connection.
            optionList.Connection = IntPtr.Zero;

            optionList.OptionCount = _optionlist.Count;
            optionList.OptionError = 0;
            int optionListSize = Marshal.SizeOf(optionList);

            // Allocate memory for the INTERNET_PER_CONN_OPTION_LIST instance.
            IntPtr intptrStruct = Marshal.AllocCoTaskMem(optionListSize);

            // Marshal data from a managed object to an unmanaged block of memory.
            Marshal.StructureToPtr(optionList, intptrStruct, true);

            // Set internet settings.
            bool bReturn = NativeMethods.InternetSetOption(
                IntPtr.Zero, 
                INTERNET_OPTION.INTERNET_OPTION_PER_CONNECTION_OPTION,
                intptrStruct, optionListSize);

            // Free the allocated memory.
            Marshal.FreeCoTaskMem(buffer);
            Marshal.FreeCoTaskMem(intptrStruct);

            // Throw an exception if this operation failed.
            if (!bReturn)
            {
                throw new Exception("InternetSetOption: " + Marshal.GetLastWin32Error());
            }

            // Notify the system that the registry settings have been changed and cause
            // the proxy data to be reread from the registry for a handle.
            NativeMethods.InternetSetOption(
                IntPtr.Zero,
                INTERNET_OPTION.INTERNET_OPTION_SETTINGS_CHANGED,
                IntPtr.Zero, 0);

            NativeMethods.InternetSetOption(
                IntPtr.Zero,
                INTERNET_OPTION.INTERNET_OPTION_REFRESH,
                IntPtr.Zero, 0);
        }
    }
}
