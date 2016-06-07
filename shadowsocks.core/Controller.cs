using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Shadowsocks.Models;
using Shadowsocks.Services;

namespace Shadowsocks
{
    public class Controller
    {
        private Listener _listener;
        private PACServer _pacServer;
        private PrivoxyRunner _privoxyRunner;

        public long inboundCounter = 0;
        public long outboundCounter = 0;

        private bool stopped = false;

        private bool _systemProxyIsDirty = false;

        #region ReleaseMemory
        private System.Threading.Timer _tmrReleaseMemory;
        private void StartReleasingMemory(int period = 30)
        {
            _tmrReleaseMemory = new System.Threading.Timer(ReleaseMemory, null, 0, period * 1000);
        }

        private void ReleaseMemory(object sender)
        {
            Utils.ReleaseMemory(false);
        }
        #endregion

        #region Events
        public class PathEventArgs : EventArgs
        {
            public string Path;
        }

        public event EventHandler ConfigChanged;
        public event EventHandler EnableStatusChanged;
        public event EventHandler EnableGlobalChanged;
        public event EventHandler ShareOverLANStatusChanged;

        // when user clicked Edit PAC, and PAC file has already created
        public event EventHandler<PathEventArgs> PACFileReadyToOpen;
        public event EventHandler<PathEventArgs> UserRuleFileReadyToOpen;

        public event ErrorEventHandler Errored;
        #endregion

        #region Body
        private Configuration _config;

        public Controller()
        {
            _config = Configuration.Load();
            StartReleasingMemory();
        }
        public void Start()
        {
            Reload();
        }
        public void Stop()
        {
            if (stopped)
            {
                return;
            }
            stopped = true;
            _listener?.Stop();
            _privoxyRunner?.Stop();
            if (_config.enabled)
            {
                SystemProxy.Update(_config, true);
            }
        }

        protected void Reload()
        {
            // some logic in configuration updated the config when saving, we need to read it again
            _config = Configuration.Load();

            if (_privoxyRunner == null)
            {
                _privoxyRunner = new PrivoxyRunner();
            }
            if (_pacServer == null)
            {
                _pacServer = new PACServer();
                _pacServer.PACFileChanged += pacServer_PACFileChanged;
                _pacServer.UserRuleFileChanged += pacServer_UserRuleFileChanged;
            }
            _pacServer.UpdateConfiguration(_config);

            _listener?.Stop();

            // don't put polipoRunner.Start() before pacServer.Stop()
            // or bind will fail when switching bind address from 0.0.0.0 to 127.0.0.1
            // though UseShellExecute is set to true now
            // http://stackoverflow.com/questions/10235093/socket-doesnt-close-after-application-exits-if-a-launched-process-is-open
            _privoxyRunner.Stop();
            try
            {
                _privoxyRunner.Start(_config);

                TCPRelay tcpRelay = new TCPRelay(this);
                UDPRelay udpRelay = new UDPRelay(this);
                List<Listener.Service> services = new List<Listener.Service> {tcpRelay, udpRelay, _pacServer, new PortForwarder(_privoxyRunner.RunningPort)};
                _listener = new Listener(services);
                _listener.Start(_config);
            }
            catch (Exception e)
            {
                // translate Microsoft language into human language
                // i.e. An attempt was made to access a socket in a way forbidden by its access permissions => Port already in use
                if (e is SocketException)
                {
                    SocketException se = (SocketException)e;
                    if (se.SocketErrorCode == SocketError.AccessDenied)
                    {
                        e = new Exception("Port already in use", e);
                    }
                }
                Logging.LogUsefulException(e);
                ReportError(e);
            }

            ConfigChanged?.Invoke(this, new EventArgs());

            UpdateSystemProxy();
            Utils.ReleaseMemory(true);
        }

        protected void ReportError(Exception e)
        {
            Errored?.Invoke(this, new ErrorEventArgs(e));
        }

        public Server GetCurrentServer()
        {
            return _config.GetCurrentServer();
        }

        // always return copy
        public Configuration GetConfigurationCopy()
        {
            return Configuration.Load();
        }

        // always return current instance
        public Configuration GetCurrentConfiguration()
        {
            return _config;
        }

        public void SaveServers(List<Server> servers, int localPort)
        {
            _config.configs = servers;
            _config.localPort = localPort;
            Configuration.Save(_config);
        }

        public bool AddServerBySSURL(string ssURL)
        {
            try
            {
                var server = new Server(ssURL);
                _config.configs.Add(server);
                _config.index = _config.configs.Count - 1;
                SaveConfig(_config);
                return true;
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                return false;
            }
        }

        public void ToggleEnable(bool enabled)
        {
            _config.enabled = enabled;
            UpdateSystemProxy();
            SaveConfig(_config);
            EnableStatusChanged?.Invoke(this, new EventArgs());
        }

        public void ToggleGlobal(bool global)
        {
            _config.global = global;
            UpdateSystemProxy();
            SaveConfig(_config);
            EnableGlobalChanged?.Invoke(this, new EventArgs());
        }

        public void ToggleShareOverLAN(bool enabled)
        {
            _config.shareOverLan = enabled;
            SaveConfig(_config);
            ShareOverLANStatusChanged?.Invoke(this, new EventArgs());
        }

        public void SelectServerIndex(int index)
        {
            _config.index = index;
            SaveConfig(_config);
        }


        public void TouchPACFile()
        {
            string pacFilename = _pacServer.TouchPACFile();
            PACFileReadyToOpen?.Invoke(this, new PathEventArgs() { Path = pacFilename });
        }

        public void TouchUserRuleFile()
        {
            string userRuleFilename = _pacServer.TouchUserRuleFile();
            UserRuleFileReadyToOpen?.Invoke(this, new PathEventArgs() { Path = userRuleFilename });
        }

        public string GetSSUrlForCurrentServer()
        {
            Server server = GetCurrentServer();
            return GetSSUrl(server);
        }

        public static string GetSSUrl(Server server)
        {
            string parts = server.method + ":" + server.password + "@" + server.server + ":" + server.server_port;
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(parts));
            return "ss://" + base64;
        }

        public Bitmap GetQRCodeForCurrentServer(int height = 1024)
        {
            Server server = GetCurrentServer();
            return GetQRCode(server,height);
        }

        public static Bitmap GetQRCode(Server server,int height)
        {
            string parts = server.method + ":" + server.password + "@" + server.server + ":" + server.server_port;
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(parts));
            var code = ZXing.QrCode.Internal.Encoder.encode(base64, ZXing.QrCode.Internal.ErrorCorrectionLevel.M);
            var m = code.Matrix;
            int blockSize = Math.Max(height / m.Height, 1);
            Bitmap drawArea = new Bitmap((m.Width * blockSize), (m.Height * blockSize));
            using (Graphics g = Graphics.FromImage(drawArea))
            {
                g.Clear(Color.White);
                using (Brush b = new SolidBrush(Color.Black))
                {
                    for (int row = 0; row < m.Width; row++)
                    {
                        for (int col = 0; col < m.Height; col++)
                        {
                            if (m[row, col] != 0)
                            {
                                g.FillRectangle(b, blockSize * row, blockSize * col, blockSize, blockSize);
                            }
                        }
                    }
                }
            }
            return drawArea;
        }

        public void SavePACUrl(string pacUrl)
        {
            _config.pacUrl = pacUrl;
            UpdateSystemProxy();
            SaveConfig(_config);
            ConfigChanged?.Invoke(this, new EventArgs());
        }

        public void UseOnlinePAC(bool useOnlinePac)
        {
            _config.useOnlinePac = useOnlinePac;
            UpdateSystemProxy();
            SaveConfig(_config);
            ConfigChanged?.Invoke(this, new EventArgs());
        }

        protected void SaveConfig(Configuration newConfig)
        {
            Configuration.Save(newConfig);
            Reload();
        }

        private void UpdateSystemProxy()
        {
            if (_config.enabled)
            {
                SystemProxy.Update(_config, false);
                _systemProxyIsDirty = true;
            }
            else
            {
                // only switch it off if we have switched it on
                if (_systemProxyIsDirty)
                {
                    SystemProxy.Update(_config, false);
                    _systemProxyIsDirty = false;
                }
            }
        }

        private void pacServer_PACFileChanged(object sender, EventArgs e)
        {
            UpdateSystemProxy();
        }

        public void UpdateLatency(Server server, TimeSpan latency)
        {
            //TODO
        }

        public void UpdateInboundCounter(Server server, long n)
        {
            //TODO
        }

        public void UpdateOutboundCounter(Server server, long n)
        {
            //TODO
        }
        #endregion

        #region GFWList
        private async void pacServer_UserRuleFileChanged(object sender, EventArgs e)
        {
            if (!File.Exists(Utils.GetTempPath("gfwlist.txt")))
            {
                await UpdateGFWListAsync().ConfigureAwait(false);
                return;
            }
            PushGFWList(File.ReadAllText(Utils.GetTempPath("gfwlist.txt")));
        }
        private static readonly IEnumerable<char> IgnoredLineBegins = new[] { '!', '[' };
        private const string GFWLIST_URL = "https://raw.githubusercontent.com/gfwlist/gfwlist/master/gfwlist.txt";

        public async Task UpdateGFWListAsync()
        {
            var http = new System.Net.WebClient { Proxy = new System.Net.WebProxy(System.Net.IPAddress.Loopback.ToString(), _config.localPort) };
            PushGFWList(await http.DownloadStringTaskAsync(new Uri(GFWLIST_URL)).ConfigureAwait(false));
        }
        private void PushGFWList(string content)
        {
            File.WriteAllText(Utils.GetTempPath("gfwlist.txt"), content, Encoding.UTF8);
            List<string> lines = ParseGFWList(content);
            if (File.Exists(PACServer.USER_RULE_FILE))
            {
                string local = File.ReadAllText(PACServer.USER_RULE_FILE, Encoding.UTF8);
                using (var sr = new StringReader(local))
                {
                    foreach (var rule in sr.NonWhiteSpaceLines())
                    {
                        if (rule.BeginWithAny(IgnoredLineBegins))
                            continue;
                        lines.Add(rule);
                    }
                }
            }
            var abpContent = File.Exists(PACServer.USER_ABP_FILE) ? File.ReadAllText(PACServer.USER_ABP_FILE, Encoding.UTF8) : Utils.UnGzip(Core.Properties.Resources.abp_js);
            abpContent = abpContent.Replace("__RULES__", SimpleJson.SimpleJson.SerializeObject(lines));
            if (File.Exists(PACServer.PAC_FILE))
            {
                string original = File.ReadAllText(PACServer.PAC_FILE, Encoding.UTF8);
                if (original == abpContent)
                {
                    return;
                }
            }
            File.WriteAllText(PACServer.PAC_FILE, abpContent, Encoding.UTF8);
        }
        private static List<string> ParseGFWList(string response)
        {
            byte[] bytes = Convert.FromBase64String(response);
            string content = Encoding.ASCII.GetString(bytes);
            List<string> valid_lines = new List<string>();
            using (var sr = new StringReader(content))
            {
                foreach (var line in sr.NonWhiteSpaceLines())
                {
                    if (line.BeginWithAny(IgnoredLineBegins))
                        continue;
                    valid_lines.Add(line);
                }
            }
            return valid_lines;
        }
        #endregion
    }
}
