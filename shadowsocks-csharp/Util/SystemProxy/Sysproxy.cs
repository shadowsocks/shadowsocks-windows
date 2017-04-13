using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Shadowsocks.Controller;
using Shadowsocks.Properties;

namespace Shadowsocks.Util.SystemProxy
{
    public static class Sysproxy
    {
        private static bool _userSettingsRecorded = false;

        // In general, this won't change
        // format:
        //  <flags><CR-LF>
        //  <proxy-server><CR-LF>
        //  <bypass-list><CR-LF>
        //  <pac-url>
        private static string[] _userSettings = new string[4];

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
            string str;
            if (_userSettingsRecorded == false)
            {
                // record user settings
                ExecSysproxy("query", out str);
                ParseQueryStr(str);
                _userSettingsRecorded = true;
            }
            string arguments;

            if (enable)
            {
                arguments = global
                    ? $"global {proxyServer} <local>;localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;172.32.*;192.168.*"
                    : $"pac {pacURL}";
            }
            else
            {
                // restore user settings
                var flags = _userSettings[0];
                var proxy_server = _userSettings[1] ?? "-";
                var bypass_list = _userSettings[2] ?? "-";
                var pac_url = _userSettings[3] ?? "-";
                arguments = $"set {flags} {proxy_server} {bypass_list} {pac_url}";

                // have to get new settings
                _userSettingsRecorded = false;
            }

            ExecSysproxy(arguments, out str);
        }

        private static void ExecSysproxy(string arguments, out string queryStr)
        {
            using (var process = new Process())
            {
                // Configure the process using the StartInfo properties.
                process.StartInfo.FileName = Utils.GetTempPath("sysproxy.exe");
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = Utils.GetTempPath();
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;

                // Need to provide encoding info, or output/error strings we got will be wrong.
                process.StartInfo.StandardOutputEncoding = Encoding.Unicode;
                process.StartInfo.StandardErrorEncoding = Encoding.Unicode;

                process.StartInfo.CreateNoWindow = true;
                process.Start();

                var stderr = process.StandardError.ReadToEnd();
                var stdout = process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                var exitCode = process.ExitCode;
                if (exitCode != (int)RET_ERRORS.RET_NO_ERROR)
                {
                    throw new ProxyException(stderr);
                }

                if (arguments == "query" && stdout.IsNullOrWhiteSpace())
                {
                    // we cannot get user settings
                    throw new ProxyException("failed to query wininet settings");
                }
                queryStr = stdout;
            }
        }

        private static void ParseQueryStr(string str)
        {
            _userSettings = str.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < 4; i++)
            {
                // handle output from WinINET
                if (_userSettings[i] == "(null)")
                    _userSettings[i] = null;
            }
        }
    }
}