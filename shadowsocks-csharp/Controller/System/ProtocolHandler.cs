using Microsoft.Win32;
using NLog;
using Shadowsocks.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Controller
{
    static class ProtocolHandler
    {
        const string ssURLRegKey = @"SOFTWARE\Classes\ss";

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static bool Set(bool enabled)
        {
            RegistryKey ssURLAssociation = null;

            try
            {
                ssURLAssociation = Registry.CurrentUser.CreateSubKey(ssURLRegKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (ssURLAssociation == null)
                {
                    logger.Error(@"Failed to create HKCU\SOFTWARE\Classes\ss to register ss:// association.");
                    return false;
                }
                if (enabled)
                {
                    ssURLAssociation.SetValue("", "URL:Shadowsocks");
                    ssURLAssociation.SetValue("URL Protocol", "");
                    var shellOpen = ssURLAssociation.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command");
                    shellOpen.SetValue("", $"{Program.ExecutablePath} --open-url %1");
                    logger.Info(@"Successfully added ss:// association.");
                }
                else
                {
                    Registry.CurrentUser.DeleteSubKeyTree(ssURLRegKey);
                    logger.Info(@"Successfully removed ss:// association.");
                }
                return true;
            }
            catch (Exception e)
            {
                logger.LogUsefulException(e);
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
                    { logger.LogUsefulException(e); }
                }
            }
        }

        public static bool Check()
        {
            RegistryKey ssURLAssociation = null;
            try
            {
                ssURLAssociation = Registry.CurrentUser.OpenSubKey(ssURLRegKey, true);
                if (ssURLAssociation == null)
                {
                    //logger.Info(@"ss:// links not associated.");
                    return false;
                }

                var shellOpen = ssURLAssociation.OpenSubKey("shell").OpenSubKey("open").OpenSubKey("command");
                return (string)shellOpen.GetValue("") == $"{Program.ExecutablePath} --open-url %1";
            }
            catch (Exception e)
            {
                logger.LogUsefulException(e);
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
                    { logger.LogUsefulException(e); }
                }
            }
        }

    }
}
