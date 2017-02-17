using System.Diagnostics;
using System.Management;
using System.Text;

namespace Shadowsocks.Util.ProcessManagement
{
    static class ThreadUtil
    {

        /*
         * See:
         * http://stackoverflow.com/questions/2633628/can-i-get-command-line-arguments-of-other-processes-from-net-c
         */
        public static string GetCommandLine(this Process process)
        {
            var commandLine = new StringBuilder(process.MainModule.FileName);

            commandLine.Append(" ");
            using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            {
                foreach (var @object in searcher.Get())
                {
                    commandLine.Append(@object["CommandLine"]);
                    commandLine.Append(" ");
                }
            }

            return commandLine.ToString();
        }
    }
}
