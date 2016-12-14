using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shadowsocks.Controller;
using Shadowsocks.Properties;

namespace Shadowsocks.Util.SystemProxy
{
    public static class Sysproxy
    {

        enum RET_ERRORS : int
        {
            RET_NO_ERROR = 0,
            INVALID_FORMAT = 1,
            NO_PERMISSION = 2,
            SYSCALL_FAILED = 3,
            NO_MEMORY = 4,
            INVAILD_OPTION_COUNT = 5,
        };

        static Sysproxy()
        {
            try
            {
                FileManager.UncompressFile(Utils.GetTempPath("sysproxy.exe"),
                    Environment.Is64BitOperatingSystem ? Resources.sysproxy64_exe : Resources.sysproxy_exe);
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        public static void SetIEProxy(bool enable, bool global, string proxyServer, string pacURL)
        {
            string arguments;

            if (enable)
            {
                if (global)
                {
                    arguments = $"global {proxyServer} <local>;localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;172.32.*;192.168.*";
                }
                else
                {
                    arguments = $"pac {pacURL}";
                }
            }
            else
            {
                arguments = "off";
            }

            using (var process = new Process())
            {
                // Configure the process using the StartInfo properties.
                process.StartInfo.FileName = Utils.GetTempPath("sysproxy.exe");
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = Utils.GetTempPath();
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                var error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                var exitCode = process.ExitCode;
                if (exitCode != (int) RET_ERRORS.RET_NO_ERROR)
                {
                    throw new ProxyException(error);
                }
            }
        }
    }
}
