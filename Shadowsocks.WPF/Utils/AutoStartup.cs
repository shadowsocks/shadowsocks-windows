using Microsoft.Win32;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Shadowsocks.WPF.Utils
{
    public static class AutoStartup
    {
        private static readonly string registryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private static readonly string Key = "Shadowsocks_" + Utilities.ExecutablePath.GetHashCode();

        public static bool Set(bool enabled)
        {
            RegistryKey? runKey = null;
            try
            {
                runKey = Registry.CurrentUser.CreateSubKey(registryRunKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (runKey == null)
                {
                    LogHost.Default.Error(@"Cannot find HKCU\{registryRunKey}.");
                    return false;
                }
                if (enabled)
                {
                    runKey.SetValue(Key, Utilities.ExecutablePath);
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
                LogHost.Default.Error(e, "An error occurred while setting auto startup registry entry.");
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
                    {
                        LogHost.Default.Error(e, "An error occurred while setting auto startup registry entry.");
                    }
                }
            }
        }

        public static bool Check()
        {
            RegistryKey? runKey = null;
            try
            {
                runKey = Registry.CurrentUser.CreateSubKey(registryRunKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (runKey == null)
                {
                    LogHost.Default.Error(@"Cannot find HKCU\{registryRunKey}.");
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
                    if (Utilities.ExecutablePath.Equals(runKey.GetValue(valueName)?.ToString(), StringComparison.InvariantCultureIgnoreCase)
                        is bool matchedDuplicate && matchedDuplicate)
                    {
                        runKey.DeleteValue(valueName);
                        runKey.SetValue(Key, Utilities.ExecutablePath);
                        check = true;
                    }
                }
                return check;
            }
            catch (Exception e)
            {
                LogHost.Default.Error(e, "An error occurred while checking auto startup registry entries.");
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
                    {
                        LogHost.Default.Error(e, "An error occurred while checking auto startup registry entries.");
                    }
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
                string[] args = new List<string>(Environment.GetCommandLineArgs())
                    .Select(p => p.Replace("\"", "\\\""))                   // escape " to \"
                    .Select(p => p.IndexOf(" ") >= 0 ? "\"" + p + "\"" : p) // encapsule with "
                    .ToArray();
                string cmdline = string.Join(" ", args);
                // first parameter is process command line parameter
                // needn't include the name of the executable in the command line
                RegisterApplicationRestart(cmdline, (int)(ApplicationRestartFlags.RESTART_NO_CRASH | ApplicationRestartFlags.RESTART_NO_HANG));
                LogHost.Default.Debug("Register restart after system reboot, command line:" + cmdline);
            }
            // requested unregister, which has no side effect
            else if (!register)
            {
                UnregisterApplicationRestart();
                LogHost.Default.Debug("Unregister restart after system reboot");
            }
        }
    }
}
