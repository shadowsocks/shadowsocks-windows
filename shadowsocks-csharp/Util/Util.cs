using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using Shadowsocks.Controller;

namespace Shadowsocks.Util
{
    public class Utils
    {
        private static bool? _portableMode;
        private static string TempPath = null;

        public static bool IsPortableMode()
        {
            if (!_portableMode.HasValue)
            {
                _portableMode = File.Exists(Path.Combine(Application.StartupPath, "shadowsocks_portable_mode.txt"));
            }

            return _portableMode.Value;
        }

        // return path to store temporary files
        public static string GetTempPath()
        {
            if (TempPath == null)
            {
                if (IsPortableMode())
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(Application.StartupPath, "temp"));
                    }
                    catch (Exception e)
                    {
                        TempPath = Path.GetTempPath();
                        Logging.LogUsefulException(e);
                    }
                    finally
                    {
                        // don't use "/", it will fail when we call explorer /select xxx/temp\xxx.log
                        TempPath = Path.Combine(Application.StartupPath, "temp");
                    }
                else
                    TempPath = Path.GetTempPath();
            }
            return TempPath;
        }

        // return a full path with filename combined which pointed to the temporary directory
        public static string GetTempPath(string filename)
        {
            return Path.Combine(GetTempPath(), filename);
        }

        public static void ReleaseMemory(bool removePages)
        {
            // release any unused pages
            // making the numbers look good in task manager
            // this is totally nonsense in programming
            // but good for those users who care
            // making them happier with their everyday life
            // which is part of user experience
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
            if (removePages)
            {
                // as some users have pointed out
                // removing pages from working set will cause some IO
                // which lowered user experience for another group of users
                //
                // so we do 2 more things here to satisfy them:
                // 1. only remove pages once when configuration is changed
                // 2. add more comments here to tell users that calling
                //    this function will not be more frequent than
                //    IM apps writing chat logs, or web browsers writing cache files
                //    if they're so concerned about their disk, they should
                //    uninstall all IM apps and web browsers
                //
                // please open an issue if you're worried about anything else in your computer
                // no matter it's GPU performance, monitor contrast, audio fidelity
                // or anything else in the task manager
                // we'll do as much as we can to help you
                //
                // just kidding
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle,
                                         (UIntPtr)0xFFFFFFFF,
                                         (UIntPtr)0xFFFFFFFF);
            }
        }

        public static string UnGzip(byte[] buf)
        {
            byte[] buffer = new byte[1024];
            int n;
            using (MemoryStream sb = new MemoryStream())
            {
                using (GZipStream input = new GZipStream(new MemoryStream(buf),
                                                         CompressionMode.Decompress,
                                                         false))
                {
                    while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        sb.Write(buffer, 0, n);
                    }
                }
                return System.Text.Encoding.UTF8.GetString(sb.ToArray());
            }
        }

        public static string FormatBandwidth(long n)
        {
            var result = GetBandwidthScale(n);
            return $"{result.Item1:0.##}{result.Item2}";
        }

        public static string FormatBytes(long bytes)
        {
            const long K = 1024L;
            const long M = K * 1024L;
            const long G = M * 1024L;
            const long T = G * 1024L;
            const long P = T * 1024L;
            const long E = P * 1024L;

            if (bytes >= P * 990)
                return (bytes / (double)E).ToString("F5") + "EB";
            if (bytes >= T * 990)
                return (bytes / (double)P).ToString("F5") + "PB";
            if (bytes >= G * 990)
                return (bytes / (double)T).ToString("F5") + "TB";
            if (bytes >= M * 990)
            {
                return (bytes / (double)G).ToString("F4") + "GB";
            }
            if (bytes >= M * 100)
            {
                return (bytes / (double)M).ToString("F1") + "MB";
            }
            if (bytes >= M * 10)
            {
                return (bytes / (double)M).ToString("F2") + "MB";
            }
            if (bytes >= K * 990)
            {
                return (bytes / (double)M).ToString("F3") + "MB";
            }
            if (bytes > K * 2)
            {
                return (bytes / (double)K).ToString("F1") + "KB";
            }
            return bytes.ToString();
        }

        /// <summary>
        /// Return scaled bandwidth
        /// </summary>
        /// <param name="n">Raw bandwidth</param>
        /// <returns>
        /// Item1: float, bandwidth with suitable scale (eg. 56)
        /// Item2: string, scale unit name (eg. KiB)
        /// Item3: long, scale unit (eg. 1024)
        /// </returns>
        public static Tuple<float, string, long> GetBandwidthScale(long n)
        {
            long scale = 1;
            float f = n;
            string unit = "B";
            if (f > 1024)
            {
                f = f / 1024;
                scale <<= 10;
                unit = "KiB";
            }
            if (f > 1024)
            {
                f = f / 1024;
                scale <<= 10;
                unit = "MiB";
            }
            if (f > 1024)
            {
                f = f / 1024;
                scale <<= 10;
                unit = "GiB";
            }
            if (f > 1024)
            {
                f = f / 1024;
                scale <<= 10;
                unit = "TiB";
            }
            return new Tuple<float, string, long>(f, unit, scale);
        }

        public static RegistryKey OpenUserRegKey( string name, bool writable ) {
            // we are building x86 binary for both x86 and x64, which will
            // cause problem when opening registry key
            // detect operating system instead of CPU
            RegistryKey userKey = RegistryKey.OpenRemoteBaseKey( RegistryHive.CurrentUser, "",
                Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32 )
                .OpenSubKey( name, writable );
            return userKey;
        }

        public static bool IsWinVistaOrHigher() {
            return Environment.OSVersion.Version.Major > 5;
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessWorkingSetSize(IntPtr process,
            UIntPtr minimumWorkingSetSize, UIntPtr maximumWorkingSetSize);
    }
}
