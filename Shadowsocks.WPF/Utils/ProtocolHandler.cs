using Microsoft.Win32;
using Splat;
using System;

namespace Shadowsocks.WPF.Utils
{
    static class ProtocolHandler
    {
        const string ssURLRegKey = @"SOFTWARE\Classes\ss";

        public static bool Set(bool enabled)
        {
            RegistryKey? ssURLAssociation = null;

            try
            {
                ssURLAssociation = Registry.CurrentUser.CreateSubKey(ssURLRegKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (ssURLAssociation == null)
                {
                    LogHost.Default.Error(@"Failed to create HKCU\SOFTWARE\Classes\ss to register ss:// association.");
                    return false;
                }
                if (enabled)
                {
                    ssURLAssociation.SetValue("", "URL:Shadowsocks");
                    ssURLAssociation.SetValue("URL Protocol", "");
                    var shellOpen = ssURLAssociation.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command");
                    shellOpen.SetValue("", $"{Utilities.ExecutablePath} --open-url %1");
                    LogHost.Default.Info(@"Successfully added ss:// association.");
                }
                else
                {
                    Registry.CurrentUser.DeleteSubKeyTree(ssURLRegKey);
                    LogHost.Default.Info(@"Successfully removed ss:// association.");
                }
                return true;
            }
            catch (Exception e)
            {
                LogHost.Default.Error(e, "An error occurred while setting ss:// association registry entries.");
                return false;
            }
            finally
            {
                if (ssURLAssociation != null)
                {
                    try
                    {
                        ssURLAssociation.Close();
                        ssURLAssociation.Dispose();
                    }
                    catch (Exception e)
                    {
                        LogHost.Default.Error(e, "An error occurred while setting ss:// association registry entries.");
                    }
                }
            }
        }

        public static bool Check()
        {
            RegistryKey? ssURLAssociation = null;
            try
            {
                ssURLAssociation = Registry.CurrentUser.OpenSubKey(ssURLRegKey, true);
                if (ssURLAssociation == null)
                {
                    //logger.Info(@"ss:// links not associated.");
                    return false;
                }

                var shellOpen = ssURLAssociation.OpenSubKey("shell")?.OpenSubKey("open")?.OpenSubKey("command");
                return shellOpen?.GetValue("") as string == $"{Utilities.ExecutablePath} --open-url %1";
            }
            catch (Exception e)
            {
                LogHost.Default.Error(e, "An error occurred while checking ss:// association registry entries.");
                return false;
            }
            finally
            {
                if (ssURLAssociation != null)
                {
                    try
                    {
                        ssURLAssociation.Close();
                        ssURLAssociation.Dispose();
                    }
                    catch (Exception e)
                    {
                        LogHost.Default.Error(e, "An error occurred while checking ss:// association registry entries.");
                    }
                }
            }
        }

    }
}
