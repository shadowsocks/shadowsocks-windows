/****************************** Module Header ******************************\
 Module Name:  NativeMethods.cs
 Project:      CSWebBrowserWithProxy
 Copyright (c) Microsoft Corporation.
 
 This class is a simple .NET wrapper of wininet.dll. It contains 4 extern
 methods in wininet.dll. They are InternetOpen, InternetCloseHandle, 
 InternetSetOption and InternetQueryOption.
 
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
    internal static class NativeMethods
    {
        /// <summary>
        /// Sets an Internet option.
        /// </summary>
        [DllImport("wininet.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern bool InternetSetOption(
            IntPtr hInternet,
            INTERNET_OPTION dwOption,
            IntPtr lpBuffer,
            int lpdwBufferLength);
    }
}
