using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace Shadowsocks.Controller.Service
{
    public static class ConnectionSpeedTester
    {
        public class BackgroundThread
        {
            public BackgroundThread(ShadowsocksController controller)
            {
                Controller = controller;
                Thread = new Thread(Process);
            }

            public readonly ShadowsocksController Controller;
            public readonly Thread Thread;
            internal StreamWriter Writer;
            internal volatile bool Stopped;

            private void Process()
            {
                var config = Controller.GetCurrentConfiguration();
                var proxy = new WebProxy(new Uri("http://localhost:" + config.localPort));
                var servers = config.configs;
                int i = 0, max = 0, count = servers.Count;
                var maxSpeed = 0.0;
                while (i < count) if (!Stopped)
                    try
                    {
                        var server = servers[i];
                        Writer.WriteLine($"Testing {server.FriendlyName()}...");
                        Controller.SelectServerIndex(i);
                        var tester = new DownloadThread(proxy);
                        var stopwatch = Stopwatch.StartNew();
                        tester.Thread.Start();
                        if (!tester.Thread.Join(5000)) tester.Stopped = true;
                        stopwatch.Stop();
                        if (Stopped) break;
                        var secs = stopwatch.Elapsed.TotalSeconds;
                        var speed = tester.Size / secs;
                        Writer.WriteLine($"Downloaded {GetSize(tester.Size)} in {secs}s, average {GetSize(speed)}/s.");
                        server.remarks = tester.Size <= 0 ? "dead" : GetSizeShort((long)speed);
                        if (speed < maxSpeed) continue;
                        maxSpeed = speed;
                        max = i;
                    }
                    catch (Exception exc)
                    {
                        if (Stopped) break;
                        Writer.WriteLine("Failed: " + exc.Message);
                    }
                    finally
                    {
                        ++i;
                    }
                Controller.SaveServers(servers, config.localPort, config.portableMode);
                Controller.SelectServerIndex(max);
                Stopped = true;
                Writer.Dispose();
                FreeConsole();
                currentConfig = null;
            }
        }

        private class DownloadThread
        {
            public DownloadThread(IWebProxy proxy)
            {
                request = (HttpWebRequest)
                    WebRequest.Create("https://dl.google.com/tag/s/appguid%3D%7B8A69D345-D564-463C-AFF1-A69D9E530F96%7D%26iid%3D%7BF8B7F175-9454-645A-CF7A-CBB3458688B8%7D%26lang%3Den%26browser%3D4%26usagestats%3D1%26appname%3DGoogle%2520Chrome%26needsadmin%3Dprefers%26ap%3Dx64-stable-statsdef_1/chrome/install/ChromeStandaloneSetup64.exe");
                request.Proxy = proxy;
                request.ReadWriteTimeout = request.Timeout = 4000;
                Thread = new Thread(Process);
            }

            public readonly Thread Thread;
            private readonly HttpWebRequest request;
            public volatile bool Stopped;
            public long Size;

            private void Process()
            {
                try
                {
                    var buffer = new byte[4096];
                    using (var response = request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    {
                        int read;
                        while (!Stopped && (read = stream.Read(buffer, 0, 4096)) > 0) Size += read;
                    }
                }
                catch { }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();

        private static volatile BackgroundThread currentConfig;

        public static void TestConnection(BackgroundThread config)
        {
            if (currentConfig != null) return;
            currentConfig = config;
            AllocConsole();
            config.Writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            config.Writer.WriteLine("Ctrl + C to stop after finishing testing current server.");
            Console.CancelKeyPress += Stop;
            config.Thread.Start();
        }

        private static void Stop(object sender, ConsoleCancelEventArgs e)
        {
            if (currentConfig == null) return;
            e.Cancel = true;
            currentConfig.Stopped = true;
        }
        
        private static readonly string[]
            Units = { null, "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB", "NB", "DB", "CB" };

        public static string GetSize(long size, string bytes = "Bytes")
        {
            double n = size;
            byte i = 0;
            while (n > 1000)
            {
                n /= 1024;
                i++;
            }
            return i == 0 ? size.ToString("N0") + ' ' + bytes
                          : n.ToString("N") + ' ' + Units[i] + " (" + size.ToString("N0") + ' ' + bytes + ')';
        }

        public static string GetSize(double size, string bytes = "Bytes")
        {
            var n = size;
            byte i = 0;
            while (n > 1000)
            {
                n /= 1024;
                i++;
            }
            return n.ToString("N") + ' ' + (i == 0 ? bytes : Units[i] + " (" + size.ToString("N") + ' ' + bytes + ')');
        }

        public static string GetSizeShort(double size, string bytes = "Bytes")
        {
            var n = size;
            byte i = 0;
            while (n > 1000)
            {
                n /= 1024;
                i++;
            }
            return n.ToString("N") + ' ' + (i == 0 ? bytes : Units[i]);
        }
    }
}
