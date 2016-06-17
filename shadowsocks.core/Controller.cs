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
        #region Fields
        private Listener _listener;
        private readonly PACServer _pacServer;
        private readonly PrivoxyRunner _privoxyRunner;

        private bool stopped;

        private bool _systemProxyIsDirty;

        private System.Threading.Timer _tmrReleaseMemory;

        private bool _analysis;
        private readonly Dictionary<Server,long> inboundCounter = new Dictionary<Server, long>();
        private readonly Dictionary<Server, long> outboundCounter = new Dictionary<Server, long>();
        private readonly Dictionary<Server, List<TimeSpan>> latencyCounter = new Dictionary<Server, List<TimeSpan>>();
        #endregion

        #region Properties
        private readonly IConfig _config;
        //dirty hack temporary, shake it baby!
        public Server[] servers => _config.servers.ToArray();
        public string currentServer { get { return _config.currentServer; } set { _config.currentServer = value; ReportConfigChange(); } }
        public bool global { get { return _config.global; } set { _config.global = value; UpdateSystemProxy(); ReportConfigChange(); EnableGlobalChanged?.Invoke(this, new EventArgs()); } }
        public bool enabled { get { return _config.enabled; } set { _config.enabled = value; UpdateSystemProxy(); ReportConfigChange(); EnableStatusChanged?.Invoke(this, new EventArgs()); } }
        public bool shareOverLan { get { return _config.shareOverLan; } set { _config.shareOverLan = value; ReportConfigChange(); ShareOverLANStatusChanged?.Invoke(this, new EventArgs()); } }
        public bool isDefault { get { return _config.isDefault; } set { _config.isDefault = value; ReportConfigChange(); } }
        public int localPort { get { return _config.localPort; } set { _config.localPort = value; ReportConfigChange(); } }
        public string pacUrl { get { return _config.pacUrl; } set { _config.pacUrl = value; ReportConfigChange(); } }
        public bool useOnlinePac { get { return _config.useOnlinePac; } set { _config.useOnlinePac = value; ReportConfigChange(); } }
        public int releaseMemoryPeriod { get { return _config.releaseMemoryPeriod; } set { _config.releaseMemoryPeriod = value; StartReleasingMemory(); ReportConfigChange(false); } }
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

        #region EventInvoker
        protected void ReportError(Exception e)
        {
            Errored?.Invoke(this, new ErrorEventArgs(e));
        }
        protected void ReportConfigChange(bool needRefresh = true)
        {
            _config.Save();
            if(needRefresh)Refresh();
            ConfigChanged?.Invoke(this, new EventArgs());
        }
        private void pacServer_PACFileChanged(object sender, EventArgs e)
        {
            UpdateSystemProxy();
        }
        #endregion

        #region Analysis

        public void StartAnalysis()
        {
            _analysis = true;
        }

        public void EndAnalysis(out Dictionary<Server, long> InboundCounter, out Dictionary<Server, long> OutboundCounter, out Dictionary<Server, List<TimeSpan>> LatencyCounter)
        {
            _analysis = false;
            InboundCounter = inboundCounter;
            OutboundCounter = outboundCounter;
            LatencyCounter = latencyCounter;
            inboundCounter.Clear();
            outboundCounter.Clear();
            latencyCounter.Clear();
        }

        public void UpdateLatency(Server server, TimeSpan latency)
        {
            if (_analysis)
            {
                try
                {
                    if (!latencyCounter.ContainsKey(server)) latencyCounter.Add(server, new List<TimeSpan>());
                    latencyCounter[server].Add(latency);
                }
                catch (Exception ex)
                {
                    Logging.LogUsefulException(ex);
                    //HEHE
                }
            }

        }

        public void UpdateInboundCounter(Server server, long n)
        {
            if (_analysis)
            {
                try
                {
                    if (!inboundCounter.ContainsKey(server)) inboundCounter.Add(server, 0);
                    inboundCounter[server] += n;
                }
                catch (Exception ex)
                {
                    Logging.LogUsefulException(ex);
                    //HEHE
                }
            }
        }

        public void UpdateOutboundCounter(Server server, long n)
        {
            if (_analysis)
            {
                try
                {
                    if (!outboundCounter.ContainsKey(server)) latencyCounter.Add(server, new List<TimeSpan>());
                    outboundCounter[server] += n;
                }
                catch (Exception ex)
                {
                    Logging.LogUsefulException(ex);
                    //HEHE
                }
            }
        }
        #endregion

        #region Ctor
        public Controller(IConfig config)
        {
            if(config==null)throw new ArgumentNullException(nameof(config));
            _config = config;
            
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

            StartReleasingMemory();
        }
        #endregion

        #region PrivateMethods
        private void ReleaseMemory(object sender)
        {
            Utils.ReleaseMemory(false);
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
        private void StartReleasingMemory()
        {
            _tmrReleaseMemory?.Dispose();
            _tmrReleaseMemory = new System.Threading.Timer(ReleaseMemory, null, 0, (_config.releaseMemoryPeriod < 1 ? 30 : _config.releaseMemoryPeriod) * 1000);
        }

        private void Refresh()
        {
            _pacServer.UpdateConfiguration(_config);
            try
            {
                _privoxyRunner.Start(_config);

                TCPRelay tcpRelay = new TCPRelay(this);
                UDPRelay udpRelay = new UDPRelay(this);
                List<Listener.Service> services = new List<Listener.Service> { tcpRelay, udpRelay, _pacServer, new PortForwarder(_privoxyRunner.RunningPort) };
                _listener = new Listener(services);
                _listener.Start(_config);
            }
            catch (Exception e)
            {
                // translate Microsoft language into human language
                // i.e. An attempt was made to access a socket in a way forbidden by its access permissions => Port already in use
                var exception = e as SocketException;
                SocketException se = exception;
                if (se?.SocketErrorCode == SocketError.AccessDenied)
                {
                    var xe = new Exception("Port already in use", e);
                    Logging.LogUsefulException(xe);
                }
                ReportError(e);
            }
            _config.Save();
            ConfigChanged?.Invoke(this, new EventArgs());
            UpdateSystemProxy();
            Utils.ReleaseMemory(true);
        }
        #endregion

        #region PublicMethods
        public void Start()
        {
            _config.Save();
            Refresh();
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

        public Server GetCurrentServer()
        {
            return _config.GetCurrentServer();
        }

        public void SelectServer(string serverIdentity)
        {
            if(_config.servers.All(c => c.Identifier != serverIdentity))throw new Exception("Server not found");
            _config.currentServer = serverIdentity;
            Refresh();
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

        public async Task UpdateGFWListAsync(System.Net.IWebProxy proxy = null)
        {
            var http = new System.Net.WebClient {Proxy = proxy ?? new System.Net.WebProxy(System.Net.IPAddress.Loopback.ToString(), _config.localPort)};
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
