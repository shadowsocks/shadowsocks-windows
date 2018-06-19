using System;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    static class AutoStartup
    {
        // Don't use Application.ExecutablePath
        // see https://stackoverflow.com/questions/12945805/odd-c-sharp-path-issue
        private static readonly string ExecutablePath = Assembly.GetEntryAssembly().Location;

        private static string Key = "Shadowsocks_" + Application.StartupPath.GetHashCode();

        public static bool Set(bool enabled)
        {
            RegistryKey runKey = null;
            try
            {
                runKey = Utils.OpenRegKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if ( runKey == null ) {
                    Logging.Error( @"Cannot find HKCU\Software\Microsoft\Windows\CurrentVersion\Run" );
                    return false;
                }
                if (enabled)
                {
                    runKey.SetValue(Key, ExecutablePath);
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
                runKey = Utils.OpenRegKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
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
                        if (ExecutablePath.Equals(value, StringComparison.OrdinalIgnoreCase))
                        {
                            runKey.DeleteValue(item);
                            runKey.SetValue(Key, ExecutablePath);
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
