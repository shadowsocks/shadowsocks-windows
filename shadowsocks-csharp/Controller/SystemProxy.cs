using System.Windows.Forms;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Shadowsocks.Controller
{
    public class SystemProxy
    {

        [DllImport("wininet.dll")]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        public const int INTERNET_OPTION_REFRESH = 37;
        static bool _settingsReturn, _refreshReturn;

        public static void NotifyIE()
        {
            // These lines implement the Interface in the beginning of program 
            // They cause the OS to refresh the settings, causing IP to realy update
            _settingsReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            _refreshReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }

        public static void Enable()
        {
            try
            {
                RegistryKey registry =
                    Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings",
                        true);
                registry.SetValue("ProxyEnable", 0);
                registry.SetValue("ProxyServer", "");
                registry.SetValue("AutoConfigURL", "http://127.0.0.1:8093/pac?t=" + GetTimestamp(DateTime.Now));
                SystemProxy.NotifyIE();
                //Must Notify IE first, or the connections do not chanage
                CopyProxySettingFromLan();
            }
            catch (Exception)
            {
                MessageBox.Show("can not change registry!");
                throw;
            }
        }

        public static void Disable()
        {
            try
            {
                RegistryKey registry =
                    Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings",
                        true);
                registry.SetValue("ProxyEnable", 0);
                registry.SetValue("ProxyServer", "");
                registry.SetValue("AutoConfigURL", "");
                SystemProxy.NotifyIE();
                CopyProxySettingFromLan();
            }
            catch (Exception)
            {
                MessageBox.Show("can not change registry!");
                throw;
            }
        }

        private static void CopyProxySettingFromLan()
        {
            RegistryKey registry =
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections",
                    true);
            var defulatValue = registry.GetValue("DefaultConnectionSettings");
            var connections = registry.GetValueNames();
            foreach (String each in connections){
                if (!(each.Equals("DefaultConnectionSettings")
                    || each.Equals("LAN Connection")
                    || each.Equals("SavedLegacySettings")))
                {
                    //set all the connections's proxy as the lan
                    registry.SetValue(each, defulatValue);
                }
            }
            NotifyIE();
        }

        private static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }
    }
}
