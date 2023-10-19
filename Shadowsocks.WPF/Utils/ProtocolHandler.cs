using Microsoft.Win32;
using Splat;
using System;

namespace Shadowsocks.WPF.Utils;

internal static class ProtocolHandler
{
    private const string SS_URL_REG_KEY = @"SOFTWARE\Classes\ss";

    public static bool Set(bool enabled)
    {
        RegistryKey? ssUrlAssociation = null;

        try
        {
            ssUrlAssociation = Registry.CurrentUser.CreateSubKey(SS_URL_REG_KEY, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (ssUrlAssociation == null)
            {
                LogHost.Default.Error(@"Failed to create HKCU\SOFTWARE\Classes\ss to register ss:// association.");
                return false;
            }
            if (enabled)
            {
                ssUrlAssociation.SetValue("", "URL:Shadowsocks");
                ssUrlAssociation.SetValue("URL Protocol", "");
                var shellOpen = ssUrlAssociation.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command");
                shellOpen.SetValue("", $"{Utilities.ExecutablePath} --open-url %1");
                LogHost.Default.Info(@"Successfully added ss:// association.");
            }
            else
            {
                Registry.CurrentUser.DeleteSubKeyTree(SS_URL_REG_KEY);
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
            if (ssUrlAssociation != null)
            {
                try
                {
                    ssUrlAssociation.Close();
                    ssUrlAssociation.Dispose();
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
        RegistryKey? ssUrlAssociation = null;
        try
        {
            ssUrlAssociation = Registry.CurrentUser.OpenSubKey(SS_URL_REG_KEY, true);
            if (ssUrlAssociation == null)
            {
                //logger.Info(@"ss:// links not associated.");
                return false;
            }

            var shellOpen = ssUrlAssociation.OpenSubKey("shell")?.OpenSubKey("open")?.OpenSubKey("command");
            return shellOpen?.GetValue("") as string == $"{Utilities.ExecutablePath} --open-url %1";
        }
        catch (Exception e)
        {
            LogHost.Default.Error(e, "An error occurred while checking ss:// association registry entries.");
            return false;
        }
        finally
        {
            if (ssUrlAssociation != null)
            {
                try
                {
                    ssUrlAssociation.Close();
                    ssUrlAssociation.Dispose();
                }
                catch (Exception e)
                {
                    LogHost.Default.Error(e, "An error occurred while checking ss:// association registry entries.");
                }
            }
        }
    }
}