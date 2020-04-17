using Microsoft.Win32;
using NLog;
using Shadowsocks.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Controller
{
    static class ProtocolHandler
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        // Don't use Application.ExecutablePath
        // see https://stackoverflow.com/questions/12945805/odd-c-sharp-path-issue
        private static readonly string ExecutablePath = Assembly.GetEntryAssembly().Location;

        // TODO: Elevate when necessary
        public static bool Set(bool enabled)
        {
            RegistryKey runKey = null;
            try
            {
                RegistryKey hkcr = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot,
                        Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);

                runKey = hkcr.CreateSubKey("ss",RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (runKey == null)
                {
                    logger.Error(@"Cannot find HKCR\ss");
                    return false;
                }
                if (enabled)
                {
                    runKey.SetValue("", "URL:Shadowsocks");
                    runKey.SetValue("URL Protocol", "");
                    var shellOpen = runKey.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command");
                    shellOpen.SetValue("", $"{ExecutablePath} --open-url %1");

                }
                else
                {
                    hkcr.DeleteSubKeyTree("ss");
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
                runKey = Utils.OpenRegKey(@"ss", true, RegistryHive.ClassesRoot);
                if (runKey == null)
                {
                    logger.Error(@"Cannot find HKCR\ss");
                    return false;
                }

                var shellOpen = runKey.OpenSubKey("shell").OpenSubKey("open").OpenSubKey("command");
                return (string)shellOpen.GetValue("") == $"{ExecutablePath} --open-url %1";
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

    }
}
