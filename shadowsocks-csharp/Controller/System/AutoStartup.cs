using System;
using System.Windows.Forms;
using Microsoft.Win32;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    class AutoStartup
    {
        static string Key = "Shadowsocks_" + Application.StartupPath.GetHashCode();

        public static bool Set(bool enabled)
        {
            RegistryKey runKey = null;
            try
            {
                string path = Application.ExecutablePath;
                runKey = Utils.OpenUserRegKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if ( runKey == null ) {
                    Logging.Error( @"Cannot find HKCU\Software\Microsoft\Windows\CurrentVersion\Run" );
                    return false;
                }
                if (enabled)
                {
                    runKey.SetValue(Key, path);
                }
                else
                {
                    runKey.DeleteValue(Key);
                }
                return true;
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                return false;
            }
            finally
            {
                if (runKey != null)
                {
                    try {
                        runKey.Close();
                        runKey.Dispose();
                    } catch (Exception e)
                    { Logging.LogUsefulException(e); }
                }
            }
        }

        public static bool Check()
        {
            RegistryKey runKey = null;
            try
            {
                string path = Application.ExecutablePath;
                runKey = Utils.OpenUserRegKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (runKey == null) {
                    Logging.Error(@"Cannot find HKCU\Software\Microsoft\Windows\CurrentVersion\Run");
                    return false;
                }
                string[] runList = runKey.GetValueNames();
                foreach (string item in runList)
                {
                    if (item.Equals(Key, StringComparison.OrdinalIgnoreCase))
                        return true;
                    else if (item.Equals("Shadowsocks", StringComparison.OrdinalIgnoreCase)) // Compatibility with older versions
                    {
                        string value = Convert.ToString(runKey.GetValue(item));
                        if (path.Equals(value, StringComparison.OrdinalIgnoreCase))
                        {
                            runKey.DeleteValue(item);
                            runKey.SetValue(Key, path);
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                return false;
            }
            finally
            {
                if (runKey != null)
                {
                    try {
                        runKey.Close();
                        runKey.Dispose();
                    } catch (Exception e)
                    { Logging.LogUsefulException(e); }
                }
            }
        }
    }
}
