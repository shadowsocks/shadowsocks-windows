using Newtonsoft.Json;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Shadowsocks.Util.SystemProxy
{
    public static class Sysproxy
    {
        private const string _userWininetConfigFile = "user-wininet.json";

        private readonly static string[] _lanIP = {
            "<local>",
            "localhost",
            "127.*",
            "10.*",
            "172.16.*",
            "172.17.*",
            "172.18.*",
            "172.19.*",
            "172.20.*",
            "172.21.*",
            "172.22.*",
            "172.23.*",
            "172.24.*",
            "172.25.*",
            "172.26.*",
            "172.27.*",
            "172.28.*",
            "172.29.*",
            "172.30.*",
            "172.31.*",
            "192.168.*"
            };


        private static string _queryStr;

        // In general, this won't change
        // format:
        //  <flags><CR-LF>
        //  <proxy-server><CR-LF>
        //  <bypass-list><CR-LF>
        //  <pac-url>
        private static SysproxyConfig _userSettings = null;

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
            Read();

            if (!_userSettings.UserSettingsRecorded)
            {
                // record user settings
                ExecSysproxy("query");
                ParseQueryStr(_queryStr);
            }

            string arguments;
            if (enable)
            {
                string customBypassString = _userSettings.BypassList ?? "";
                List<string> customBypassList = new List<string>(customBypassString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                customBypassList.AddRange(_lanIP);
                string[] realBypassList = customBypassList.Distinct().ToArray();
                string realBypassString = string.Join(";", realBypassList);

                arguments = global
                    ? $"global {proxyServer} {realBypassString}"
                    : $"pac {pacURL}";
            }
            else
            {
                // restore user settings
                var flags = _userSettings.Flags;
                var proxy_server = _userSettings.ProxyServer ?? "-";
                var bypass_list = _userSettings.BypassList ?? "-";
                var pac_url = _userSettings.PacUrl ?? "-";
                arguments = $"set {flags} {proxy_server} {bypass_list} {pac_url}";

                // have to get new settings
                _userSettings.UserSettingsRecorded = false;
            }

            Save();
            ExecSysproxy(arguments);
        }

        // set system proxy to 1 (null) (null) (null)
        public static bool ResetIEProxy()
        {
            try
            {
                // clear user-wininet.json
                _userSettings = new SysproxyConfig();
                Save();
                // clear system setting
                ExecSysproxy("set 1 - - -");
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private static void ExecSysproxy(string arguments)
        {
            // using event to avoid hanging when redirect standard output/error
            // ref: https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
            // and http://blog.csdn.net/zhangweixing0/article/details/7356841
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
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

                    StringBuilder output = new StringBuilder();
                    StringBuilder error = new StringBuilder();

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };
                    try
                    {
                        process.Start();

                        process.BeginErrorReadLine();
                        process.BeginOutputReadLine();

                        process.WaitForExit();
                    }
                    catch (System.ComponentModel.Win32Exception e)
                    {
                        // log the arguments
                        throw new ProxyException(ProxyExceptionType.FailToRun, process.StartInfo.Arguments, e);
                    }
                    var stderr = error.ToString();
                    var stdout = output.ToString();

                    var exitCode = process.ExitCode;
                    if (exitCode != (int)RET_ERRORS.RET_NO_ERROR)
                    {
                        throw new ProxyException(ProxyExceptionType.SysproxyExitError, stderr);
                    }

                    if (arguments == "query")
                    {
                        if (stdout.IsNullOrWhiteSpace() || stdout.IsNullOrEmpty())
                        {
                            // we cannot get user settings
                            throw new ProxyException(ProxyExceptionType.QueryReturnEmpty);
                        }
                        _queryStr = stdout;
                    }
                }
            }
        }

        private static void Save()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Utils.GetTempPath(_userWininetConfigFile), FileMode.Create)))
                {
                    string jsonString = JsonConvert.SerializeObject(_userSettings, Formatting.Indented);
                    sw.Write(jsonString);
                    sw.Flush();
                }
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private static void Read()
        {
            try
            {
                string configContent = File.ReadAllText(Utils.GetTempPath(_userWininetConfigFile));
                _userSettings = JsonConvert.DeserializeObject<SysproxyConfig>(configContent);
            }
            catch (Exception)
            {
                // Suppress all exceptions. finally block will initialize new user config settings.
            }
            finally
            {
                if (_userSettings == null) _userSettings = new SysproxyConfig();
            }
        }

        private static void ParseQueryStr(string str)
        {
            string[] userSettingsArr = str.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            // sometimes sysproxy output in utf16le instead of ascii
            // manually translate it
            if (userSettingsArr.Length != 4)
            {
                byte[] strByte = Encoding.ASCII.GetBytes(str);
                str = Encoding.Unicode.GetString(strByte);
                userSettingsArr = str.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                // still fail, throw exception with string hexdump
                if (userSettingsArr.Length != 4)
                {
                    throw new ProxyException(ProxyExceptionType.QueryReturnMalformed, BitConverter.ToString(strByte));
                }
            }

            _userSettings.Flags = userSettingsArr[0];

            // handle output from WinINET
            if (userSettingsArr[1] == "(null)") _userSettings.ProxyServer = null;
            else _userSettings.ProxyServer = userSettingsArr[1];
            if (userSettingsArr[2] == "(null)") _userSettings.BypassList = null;
            else _userSettings.BypassList = userSettingsArr[2];
            if (userSettingsArr[3] == "(null)") _userSettings.PacUrl = null;
            else _userSettings.PacUrl = userSettingsArr[3];

            _userSettings.UserSettingsRecorded = true;
        }
    }
}
