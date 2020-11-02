using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NLog;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    static class AutoStartup
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        // Don't use Application.ExecutablePath
        // see https://stackoverflow.com/questions/12945805/odd-c-sharp-path-issue

        private static string Key = "Shadowsocks_" + Program.ExecutablePath.GetHashCode();

        public static bool Set(bool enabled)
        {
            RegistryKey runKey = null;
            try
            {
                runKey = Utils.OpenRegKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (runKey == null)
                {
                    logger.Error(@"Cannot find HKCU\Software\Microsoft\Windows\CurrentVersion\Run");
                    return false;
                }
                if (enabled)
                {
                    runKey.SetValue(Key, Program.ExecutablePath);
                }
                else
                {
                    runKey.DeleteValue(Key);
                }
                // When autostartup setting change, change RegisterForRestart state to avoid start 2 times
                RegisterForRestart(!enabled);
                return true;
            }
            catch (Exception e)
            {
                logger.LogUsefulException(e);
                return false;
            }
            finally
            {
                if (runKey != null)
                {
                    try
                    {
                        runKey.Close();
                        runKey.Dispose();
                    }
                    catch (Exception e)
                    { logger.LogUsefulException(e); }
                }
            }
        }

        public static bool Check()
        {
            RegistryKey runKey = null;
            try
            {
                runKey = Utils.OpenRegKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (runKey == null)
                {
                    logger.Error(@"Cannot find HKCU\Software\Microsoft\Windows\CurrentVersion\Run");
                    return false;
                }
                var check = false;
                foreach (var valueName in runKey.GetValueNames())
                {
                    if (valueName.Equals(Key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        check = true;
                        continue;
                    }

                    // Remove other startup keys with the same executable path. fixes #3011 and also assures compatibility with older versions
                    if (Program.ExecutablePath.Equals(runKey.GetValue(valueName).ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        runKey.DeleteValue(valueName);
                        runKey.SetValue(Key, Program.ExecutablePath);
                        check = true;
                    }
                }
                return check;
            }
            catch (Exception e)
            {
                logger.LogUsefulException(e);
                return false;
            }
            finally
            {
                if (runKey != null)
                {
                    try
                    {
                        runKey.Close();
                        runKey.Dispose();
                    }
                    catch (Exception e)
                    { logger.LogUsefulException(e); }
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int RegisterApplicationRestart([MarshalAs(UnmanagedType.LPWStr)] string commandLineArgs, int Flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int UnregisterApplicationRestart();

        [Flags]
        enum ApplicationRestartFlags
        {
            RESTART_ALWAYS = 0,
            RESTART_NO_CRASH = 1,
            RESTART_NO_HANG = 2,
            RESTART_NO_PATCH = 4,
            RESTART_NO_REBOOT = 8,
        }

        // register restart after system reboot/update
        public static void RegisterForRestart(bool register)
        {
            // requested register and not autostartup
            if (register && !Check())
            {
                // escape command line parameter
                string[] args = new List<string>(Program.Args)
                    .Select(p => p.Replace("\"", "\\\""))                   // escape " to \"
                    .Select(p => p.IndexOf(" ") >= 0 ? "\"" + p + "\"" : p) // encapsule with "
                    .ToArray();
                string cmdline = string.Join(" ", args);
                // first parameter is process command line parameter
                // needn't include the name of the executable in the command line
                RegisterApplicationRestart(cmdline, (int)(ApplicationRestartFlags.RESTART_NO_CRASH | ApplicationRestartFlags.RESTART_NO_HANG));
                logger.Debug("Register restart after system reboot, command line:" + cmdline);
            }
            // requested unregister, which has no side effect
            else if (!register)
            {
                UnregisterApplicationRestart();
                logger.Debug("Unregister restart after system reboot");
            }
        }
    }
}
