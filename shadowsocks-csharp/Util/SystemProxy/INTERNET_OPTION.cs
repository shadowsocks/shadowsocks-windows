/****************************** Module Header ******************************\
 Module Name:  INTERNET_OPTION.cs
 Project:      CSWebBrowserWithProxy
 Copyright (c) Microsoft Corporation.
 
 This enum contains 4 WinINet constants used in method InternetQueryOption and 
 InternetSetOption functions. 
 Visit http://msdn.microsoft.com/en-us/library/aa385328(VS.85).aspx to get the 
 whole constants list.
 
 This source is subject to the Microsoft Public License.
 See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
 All other rights reserved.
 
 THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
 EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
 WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\***************************************************************************/

namespace Shadowsocks.Util.SystemProxy
{
    public enum INTERNET_OPTION
    {
        // Sets or retrieves an INTERNET_PER_CONN_OPTION_LIST structure that specifies
        // a list of options for a particular connection.
        INTERNET_OPTION_PER_CONNECTION_OPTION = 75,

        // Notify the system that the registry settings have been changed so that
        // it verifies the settings on the next call to InternetConnect.
        INTERNET_OPTION_SETTINGS_CHANGED = 39,

        // Causes the proxy data to be reread from the registry for a handle.
        INTERNET_OPTION_REFRESH = 37

    }
}
