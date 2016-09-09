using System.Windows.Forms;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using Shadowsocks.Model;

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

        public static void RegistrySetValue(RegistryKey registry, string name, object value)
        {
            try
            {
                registry.SetValue(name, value);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }
        public static RegistryKey OpenUserRegKey(string name, bool writable)
        {
            // we are building x86 binary for both x86 and x64, which will
            // cause problem when opening registry key
            // detect operating system instead of CPU
#if _DOTNET_4_0
            RegistryKey userKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.CurrentUser, "",
                Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32
                ).OpenSubKey(name, writable);
#else
            RegistryKey userKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.CurrentUser, ""
                ).OpenSubKey(name, writable);
#endif
            return userKey;
        }

        public static void Update(Configuration config, bool forceDisable)
        {
            bool global = config.sysProxyMode == (int)ProxyMode.Global;
            bool enabled = config.sysProxyMode != (int)ProxyMode.NoModify;
            if (forceDisable)
            {
                enabled = false;
            }
            RegistryKey registry = null;
            try
            {
                registry = OpenUserRegKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true);
                if (enabled)
                {
                    if (global)
                    {
                        RegistrySetValue(registry, "ProxyEnable", 1);
                        RegistrySetValue(registry, "ProxyServer", "127.0.0.1:" + config.localPort.ToString());
                        RegistrySetValue(registry, "AutoConfigURL", "");
                    }
                    else
                    {
                        string pacUrl;
                        pacUrl = "http://127.0.0.1:" + config.localPort.ToString() + "/pac?t=" + GetTimestamp(DateTime.Now);
                        RegistrySetValue(registry, "ProxyEnable", 0);
                        RegistrySetValue(registry, "ProxyServer", "");
                        RegistrySetValue(registry, "AutoConfigURL", pacUrl);
                    }
                }
                else
                {
                    RegistrySetValue(registry, "ProxyEnable", 0);
                    RegistrySetValue(registry, "ProxyServer", "");
                    RegistrySetValue(registry, "AutoConfigURL", "");
                }
                //Set AutoDetectProxy Off
                IEAutoDetectProxy(false);
                SystemProxy.NotifyIE();
                //Must Notify IE first, or the connections do not chanage
                CopyProxySettingFromLan();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                // TODO this should be moved into views
                MessageBox.Show(I18N.GetString("Failed to update registry"));
            }
            finally
            {
                if (registry != null)
                {
                    try
                    {
                        registry.Close();
                    }
                    catch (Exception e)
                    {
                        Logging.LogUsefulException(e);
                    }
                }
            }
        }

        private static void CopyProxySettingFromLan()
        {
            RegistryKey registry = null;
            try
            {
                registry = OpenUserRegKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections", true);
                var defaultValue = registry.GetValue("DefaultConnectionSettings");
                var connections = registry.GetValueNames();
                foreach (String each in connections)
                {
                    switch (each.ToUpperInvariant())
                    {
                        case "DEFAULTCONNECTIONSETTINGS":
                        case "LAN CONNECTION":
                        case "SAVEDLEGACYSETTINGS":
                            continue;
                        default:
                            //set all the connections's proxy as the lan
                            registry.SetValue(each, defaultValue);
                            continue;
                    }
                }
                SystemProxy.NotifyIE();
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
            finally
            {
                if (registry != null)
                {
                    try
                    {
                        registry.Close();
                    }
                    catch (Exception e)
                    {
                        Logging.LogUsefulException(e);
                    }
                }
            }
        }

        private static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        /// <summary>
        /// Checks or unchecks the IE Options Connection setting of "Automatically detect Proxy"
        /// </summary>
        /// <param name="set">Provide 'true' if you want to check the 'Automatically detect Proxy' check box. To uncheck, pass 'false'</param>
        private static void IEAutoDetectProxy(bool set)
        {
            RegistryKey registry = null;
            try
            {
                registry = OpenUserRegKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections",
                        true);
                byte[] defConnection = (byte[])registry.GetValue("DefaultConnectionSettings");
                if (defConnection == null)
                {
                    defConnection = new byte[32];
                    defConnection[0] = 0x46;
                    defConnection[8] = 0x1;
                }
                byte[] savedLegacySetting = (byte[])registry.GetValue("SavedLegacySettings");
                if (savedLegacySetting == null)
                {
                    savedLegacySetting = new byte[32];
                    savedLegacySetting[0] = 0x46;
                    savedLegacySetting[8] = 0x1;
                }
                if (set)
                {
                    defConnection[8] = Convert.ToByte(defConnection[8] & 8);
                    savedLegacySetting[8] = Convert.ToByte(savedLegacySetting[8] & 8);
                }
                else
                {
                    defConnection[8] = Convert.ToByte(defConnection[8] & ~8);
                    savedLegacySetting[8] = Convert.ToByte(savedLegacySetting[8] & ~8);
                }
                RegistrySetValue(registry, "DefaultConnectionSettings", defConnection);
                RegistrySetValue(registry, "SavedLegacySettings", savedLegacySetting);
            }
            finally
            {
                if (registry != null)
                {
                    try
                    {
                        registry.Close();
                    }
                    catch (Exception e)
                    {
                        Logging.LogUsefulException(e);
                    }
                }
            }
        }
    }
}
