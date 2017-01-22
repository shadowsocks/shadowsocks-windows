using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Shadowsocks.Controller
{
    class AutoStartup
    {
        static string Key = "ShadowsocksR_" + Application.StartupPath.GetHashCode();
        static string RegistryRunPath = (IntPtr.Size == 4 ? @"Software\Microsoft\Windows\CurrentVersion\Run" : @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run");

        public static bool Set(bool enabled)
        {
            RegistryKey runKey = null;
            try
            {
                string path = Util.Utils.GetExecutablePath();
                runKey = Registry.LocalMachine.OpenSubKey(RegistryRunPath, true);
                if (enabled)
                {
                    runKey.SetValue(Key, path);
                }
                else
                {
                    runKey.DeleteValue(Key);
                }
                runKey.Close();
                return true;
            }
            catch //(Exception e)
            {
                //Logging.LogUsefulException(e);
                return Util.Utils.RunAsAdmin("--setautorun") == 0;
            }
            finally
            {
                if (runKey != null)
                {
                    try
                    {
                        runKey.Close();
                    }
                    catch (Exception e)
                    {
                        Logging.LogUsefulException(e);
                    }
                }
            }
        }

        public static bool Switch()
        {
            bool enabled = !Check();
            RegistryKey runKey = null;
            try
            {
                string path = Util.Utils.GetExecutablePath();
                runKey = Registry.LocalMachine.OpenSubKey(RegistryRunPath, true);
                if (enabled)
                {
                    runKey.SetValue(Key, path);
                }
                else
                {
                    runKey.DeleteValue(Key);
                }
                runKey.Close();
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
                    try
                    {
                        runKey.Close();
                    }
                    catch (Exception e)
                    {
                        Logging.LogUsefulException(e);
                    }
                }
            }
        }

        public static bool Check()
        {
            RegistryKey runKey = null;
            try
            {
                string path = Util.Utils.GetExecutablePath();
                runKey = Registry.LocalMachine.OpenSubKey(RegistryRunPath, false);
                string[] runList = runKey.GetValueNames();
                runKey.Close();
                foreach (string item in runList)
                {
                    if (item.Equals(Key))
                        return true;
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
                    try
                    {
                        runKey.Close();
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
