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
            int sysProxyMode = config.sysProxyMode;
            if (forceDisable)
            {
                sysProxyMode = (int)ProxyMode.NoModify;
            }
            bool global = sysProxyMode == (int)ProxyMode.Global;
            bool enabled = sysProxyMode != (int)ProxyMode.NoModify;
            using (RegistryKey registry = OpenUserRegKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
            {
                try
                {
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
                            pacUrl = "http://127.0.0.1:" + config.localPort.ToString() + "/pac?" + "auth=" + config.localAuthPassword + "&t=" + Util.Utils.GetTimestamp(DateTime.Now);
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
                    IEProxyUpdate(config, sysProxyMode);
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
            }
        }

        private static void CopyProxySettingFromLan()
        {
            using (RegistryKey registry = OpenUserRegKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections", true))
            {
                try
                {
                    var defaultValue = registry.GetValue("DefaultConnectionSettings");
                    var connections = registry.GetValueNames();
                    foreach (String each in connections)
                    {
                        switch (each.ToUpperInvariant())
                        {
                            case "DEFAULTCONNECTIONSETTINGS":
                            case "SAVEDLEGACYSETTINGS":
                            //case "LAN CONNECTION":
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
            }
        }

        private static void BytePushback(byte[] buffer, ref int buffer_len, int val)
        {
            BitConverter.GetBytes(val).CopyTo(buffer, buffer_len);
            buffer_len += 4;
        }

        private static void BytePushback(byte[] buffer, ref int buffer_len, string str)
        {
            BytePushback(buffer, ref buffer_len, str.Length);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
            bytes.CopyTo(buffer, buffer_len);
            buffer_len += bytes.Length;
        }

        private static byte[] GenConnectionSettings(Configuration config, int sysProxyMode, int counter)
        {
            byte[] buffer = new byte[1024];
            int buffer_len = 0;
            BytePushback(buffer, ref buffer_len, 70);
            BytePushback(buffer, ref buffer_len, counter + 1);
            if (sysProxyMode == (int)ProxyMode.NoModify)
                BytePushback(buffer, ref buffer_len, 1);
            else if (sysProxyMode == (int)ProxyMode.Pac)
                BytePushback(buffer, ref buffer_len, 5);
            else
                BytePushback(buffer, ref buffer_len, 3);

            string proxy = "127.0.0.1:" + config.localPort.ToString();
            BytePushback(buffer, ref buffer_len, proxy);

            string bypass = "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;172.32.*;192.168.*;<local>";
            BytePushback(buffer, ref buffer_len, bypass);

            string pacUrl = "";
            pacUrl = "http://127.0.0.1:" + config.localPort.ToString() + "/pac?" + "auth=" + config.localAuthPassword + "&t=" + Util.Utils.GetTimestamp(DateTime.Now);
            BytePushback(buffer, ref buffer_len, pacUrl);

            buffer_len += 0x20;

            Array.Resize(ref buffer, buffer_len);
            return buffer;
        }

        /// <summary>
        /// Checks or unchecks the IE Options Connection setting of "Automatically detect Proxy"
        /// </summary>
        private static void IEProxyUpdate(Configuration config, int sysProxyMode)
        {
            using (RegistryKey registry = OpenUserRegKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections", true))
            {
                try
                {
                    byte[] defConnection = (byte[])registry.GetValue("DefaultConnectionSettings");
                    int counter = 0;
                    if (defConnection != null && defConnection.Length >= 8)
                    {
                        counter = defConnection[4] | (defConnection[5] << 8);
                    }
                    defConnection = GenConnectionSettings(config, sysProxyMode, counter);
                    RegistrySetValue(registry, "DefaultConnectionSettings", defConnection);
                    RegistrySetValue(registry, "SavedLegacySettings", defConnection);
                }
                catch (IOException e)
                {
                    Logging.LogUsefulException(e);
                }
            }
            using (RegistryKey registry = OpenUserRegKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
            {
                try
                {
                    RegistrySetValue(registry, "ProxyOverride", "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;172.32.*;192.168.*;<local>");
                }
                catch (IOException e)
                {
                    Logging.LogUsefulException(e);
                }
            }
        }
    }
}
