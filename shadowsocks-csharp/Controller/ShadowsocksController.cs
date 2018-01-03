using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using Newtonsoft.Json;

using Shadowsocks.Controller.Strategy;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System.Linq;
using Shadowsocks.Controller.Service;
using Shadowsocks.Proxy;

namespace Shadowsocks.Controller
{
    public class ShadowsocksController
    {
        // controller:
        // handle user actions
        // manipulates UI
        // interacts with low level logic

        private Thread _ramThread;
        private Thread _trafficThread;

        private Listener _listener;
        private PACServer _pacServer;
        private Configuration _config;
        private StrategyManager _strategyManager;
        private PrivoxyRunner privoxyRunner;
        private GFWListUpdater gfwListUpdater;
        private readonly ConcurrentDictionary<Server, Sip003Plugin> _pluginsByServer;

        public AvailabilityStatistics availabilityStatistics = AvailabilityStatistics.Instance;
        public StatisticsStrategyConfiguration StatisticsConfiguration { get; private set; }

        private long _inboundCounter = 0;
        private long _outboundCounter = 0;
        public long InboundCounter => Interlocked.Read(ref _inboundCounter);
        public long OutboundCounter => Interlocked.Read(ref _outboundCounter);
        public Queue<TrafficPerSecond> trafficPerSecondQueue;

        private bool stopped = false;

        public class PathEventArgs : EventArgs
        {
            public string Path;
        }

        public class TrafficPerSecond
        {
            public long inboundCounter;
            public long outboundCounter;
            public long inboundIncreasement;
            public long outboundIncreasement;
        }

        public event EventHandler ConfigChanged;
        public event EventHandler EnableStatusChanged;
        public event EventHandler EnableGlobalChanged;
        public event EventHandler ShareOverLANStatusChanged;
        public event EventHandler VerboseLoggingStatusChanged;
        public event EventHandler TrafficChanged;

        // when user clicked Edit PAC, and PAC file has already created
        public event EventHandler<PathEventArgs> PACFileReadyToOpen;
        public event EventHandler<PathEventArgs> UserRuleFileReadyToOpen;

        public event EventHandler<GFWListUpdater.ResultEventArgs> UpdatePACFromGFWListCompleted;

        public event ErrorEventHandler UpdatePACFromGFWListError;

        public event ErrorEventHandler Errored;

        public ShadowsocksController()
        {
            _config = Configuration.Load();
            StatisticsConfiguration = StatisticsStrategyConfiguration.Load();
            _strategyManager = new StrategyManager(this);
            _pluginsByServer = new ConcurrentDictionary<Server, Sip003Plugin>();
            StartReleasingMemory();
            StartTrafficStatistics(61);
        }

        public void Start()
        {
            Reload();
        }

        protected void ReportError(Exception e)
        {
            if (Errored != null)
            {
                Errored(this, new ErrorEventArgs(e));
            }
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

        public IList<IStrategy> GetStrategies()
        {
            return _strategyManager.GetStrategies();
        }

        public IStrategy GetCurrentStrategy()
        {
            foreach (var strategy in _strategyManager.GetStrategies())
            {
                if (strategy.ID == this._config.strategy)
                {
                    return strategy;
                }
            }
            return null;
        }

        public Server GetAServer(IStrategyCallerType type, IPEndPoint localIPEndPoint, EndPoint destEndPoint)
        {
            IStrategy strategy = GetCurrentStrategy();
            if (strategy != null)
            {
                return strategy.GetAServer(type, localIPEndPoint, destEndPoint);
            }
            if (_config.index < 0)
            {
                _config.index = 0;
            }
            return GetCurrentServer();
        }

        public EndPoint GetPluginLocalEndPointIfConfigured(Server server)
        {
            var plugin = _pluginsByServer.GetOrAdd(server, Sip003Plugin.CreateIfConfigured);
            if (plugin == null)
            {
                return null;
            }

            try
            {
                if (plugin.StartIfNeeded())
                {
                    Logging.Info(
                        $"Started SIP003 plugin for {server.Identifier()} on {plugin.LocalEndPoint} - PID: {plugin.ProcessId}");
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to start SIP003 plugin: " + ex.Message);
                throw;
            }

            return plugin.LocalEndPoint;
        }

        public void SaveServers(List<Server> servers, int localPort)
        {
            _config.configs = servers;
            _config.localPort = localPort;
            Configuration.Save(_config);
        }

        public void SaveStrategyConfigurations(StatisticsStrategyConfiguration configuration)
        {
            StatisticsConfiguration = configuration;
            StatisticsStrategyConfiguration.Save(configuration);
        }

        public bool AddServerBySSURL(string ssURL)
        {
            try
            {
                if (ssURL.IsNullOrEmpty() || ssURL.IsWhiteSpace()) return false;
                var servers = Server.GetServers(ssURL);
                if (servers == null || servers.Count == 0) return false;
                foreach (var server in servers)
                {
                    _config.configs.Add(server);
                }
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
            SaveConfig(_config);
            if (EnableStatusChanged != null)
            {
                EnableStatusChanged(this, new EventArgs());
            }
        }

        public void ToggleGlobal(bool global)
        {
            _config.global = global;
            SaveConfig(_config);
            if (EnableGlobalChanged != null)
            {
                EnableGlobalChanged(this, new EventArgs());
            }
        }

        public void ToggleShareOverLAN(bool enabled)
        {
            _config.shareOverLan = enabled;
            SaveConfig(_config);
            if (ShareOverLANStatusChanged != null)
            {
                ShareOverLANStatusChanged(this, new EventArgs());
            }
        }

        public void DisableProxy()
        {
            _config.proxy.useProxy = false;
            SaveConfig(_config);
        }

        public void EnableProxy(int type, string proxy, int port, int timeout)
        {
            _config.proxy.useProxy = true;
            _config.proxy.proxyType = type;
            _config.proxy.proxyServer = proxy;
            _config.proxy.proxyPort = port;
            _config.proxy.proxyTimeout = timeout;
            SaveConfig(_config);
        }

        public void ToggleVerboseLogging(bool enabled)
        {
            _config.isVerboseLogging = enabled;
            SaveConfig(_config);
            if ( VerboseLoggingStatusChanged != null ) {
                VerboseLoggingStatusChanged(this, new EventArgs());
            }
        }

        public void SelectServerIndex(int index)
        {
            _config.index = index;
            _config.strategy = null;
            SaveConfig(_config);
        }

        public void SelectStrategy(string strategyID)
        {
            _config.index = -1;
            _config.strategy = strategyID;
            SaveConfig(_config);
        }

        public void Stop()
        {
            if (stopped)
            {
                return;
            }
            stopped = true;
            if (_listener != null)
            {
                _listener.Stop();
            }
            StopPlugins();
            if (privoxyRunner != null)
            {
                privoxyRunner.Stop();
            }
            if (_config.enabled)
            {
                SystemProxy.Update(_config, true, null);
            }
            Encryption.RNG.Close();
        }

        private void StopPlugins()
        {
            foreach (var serverAndPlugin in _pluginsByServer)
            {
                serverAndPlugin.Value?.Dispose();
            }
            _pluginsByServer.Clear();
        }

        public void TouchPACFile()
        {
            string pacFilename = _pacServer.TouchPACFile();
            if (PACFileReadyToOpen != null)
            {
                PACFileReadyToOpen(this, new PathEventArgs() { Path = pacFilename });
            }
        }

        public void TouchUserRuleFile()
        {
            string userRuleFilename = _pacServer.TouchUserRuleFile();
            if (UserRuleFileReadyToOpen != null)
            {
                UserRuleFileReadyToOpen(this, new PathEventArgs() { Path = userRuleFilename });
            }
        }

        public string GetServerURLForCurrentServer()
        {
            Server server = GetCurrentServer();
            return GetServerURL(server);
        }

        public static string GetServerURL(Server server)
        {
            string tag = string.Empty;
            string url = string.Empty;

            if (string.IsNullOrWhiteSpace(server.plugin))
            {
                // For backwards compatiblity, if no plugin, use old url format
                string parts = $"{server.method}:{server.password}@{server.server}:{server.server_port}";
                string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(parts));
                url = base64;
            }
            else
            {
                // SIP002
                string parts = $"{server.method}:{server.password}";
                string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(parts));
                string websafeBase64 = base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');

                string pluginPart = server.plugin;
                if (!string.IsNullOrWhiteSpace(server.plugin_opts))
                {
                    pluginPart += ";" + server.plugin_opts;
                }

                url = string.Format(
                    "{0}@{1}:{2}/?plugin={3}",
                    websafeBase64,
                    HttpUtility.UrlEncode(server.server, Encoding.UTF8),
                    server.server_port,
                    HttpUtility.UrlEncode(pluginPart, Encoding.UTF8));
            }

            if (!server.remarks.IsNullOrEmpty())
            {
                tag = $"#{HttpUtility.UrlEncode(server.remarks, Encoding.UTF8)}";
            }
            return $"ss://{url}{tag}";
        }

        public void UpdatePACFromGFWList()
        {
            if (gfwListUpdater != null)
            {
                gfwListUpdater.UpdatePACFromGFWList(_config);
            }
        }

        public void UpdateStatisticsConfiguration(bool enabled)
        {
            if (availabilityStatistics == null) return;
            availabilityStatistics.UpdateConfiguration(this);
            _config.availabilityStatistics = enabled;
            SaveConfig(_config);
        }

        public void SavePACUrl(string pacUrl)
        {
            _config.pacUrl = pacUrl;
            SaveConfig(_config);
            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }
        }

        public void UseOnlinePAC(bool useOnlinePac)
        {
            _config.useOnlinePac = useOnlinePac;
            SaveConfig(_config);
            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }
        }

        public void ToggleSecureLocalPac(bool enabled)
        {
            _config.secureLocalPac = enabled;
            SaveConfig(_config);
            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }
        }

        public void ToggleCheckingUpdate(bool enabled)
        {
            _config.autoCheckUpdate = enabled;
            Configuration.Save(_config);
            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }
        }

        public void ToggleCheckingPreRelease(bool enabled)
        {
            _config.checkPreRelease = enabled;
            Configuration.Save(_config);
            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }
        }

        public void SaveLogViewerConfig(LogViewerConfig newConfig)
        {
            _config.logViewer = newConfig;
            newConfig.SaveSize();
            Configuration.Save(_config);
            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }
        }

        public void SaveHotkeyConfig(HotkeyConfig newConfig)
        {
            _config.hotkey = newConfig;
            SaveConfig(_config);
            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }
        }

        public void UpdateLatency(Server server, TimeSpan latency)
        {
            if (_config.availabilityStatistics)
            {
                availabilityStatistics.UpdateLatency(server, (int)latency.TotalMilliseconds);
            }
        }

        public void UpdateInboundCounter(Server server, long n)
        {
            Interlocked.Add(ref _inboundCounter, n);
            if (_config.availabilityStatistics)
            {
                availabilityStatistics.UpdateInboundCounter(server, n);
            }
        }

        public void UpdateOutboundCounter(Server server, long n)
        {
            Interlocked.Add(ref _outboundCounter, n);
            if (_config.availabilityStatistics)
            {
                availabilityStatistics.UpdateOutboundCounter(server, n);
            }
        }

        protected void Reload()
        {
            StopPlugins();

            Encryption.RNG.Reload();
            // some logic in configuration updated the config when saving, we need to read it again
            _config = Configuration.Load();
            StatisticsConfiguration = StatisticsStrategyConfiguration.Load();

            if (privoxyRunner == null)
            {
                privoxyRunner = new PrivoxyRunner();
            }
            if (_pacServer == null)
            {
                _pacServer = new PACServer();
                _pacServer.PACFileChanged += pacServer_PACFileChanged;
                _pacServer.UserRuleFileChanged += pacServer_UserRuleFileChanged;
            }
            _pacServer.UpdateConfiguration(_config);
            if (gfwListUpdater == null)
            {
                gfwListUpdater = new GFWListUpdater();
                gfwListUpdater.UpdateCompleted += pacServer_PACUpdateCompleted;
                gfwListUpdater.Error += pacServer_PACUpdateError;
            }

            availabilityStatistics.UpdateConfiguration(this);

            if (_listener != null)
            {
                _listener.Stop();
            }
            // don't put PrivoxyRunner.Start() before pacServer.Stop()
            // or bind will fail when switching bind address from 0.0.0.0 to 127.0.0.1
            // though UseShellExecute is set to true now
            // http://stackoverflow.com/questions/10235093/socket-doesnt-close-after-application-exits-if-a-launched-process-is-open
            privoxyRunner.Stop();
            try
            {
                var strategy = GetCurrentStrategy();
                if (strategy != null)
                {
                    strategy.ReloadServers();
                }

                StartPlugins();
                privoxyRunner.Start(_config);

                TCPRelay tcpRelay = new TCPRelay(this, _config);
                UDPRelay udpRelay = new UDPRelay(this);
                List<Listener.IService> services = new List<Listener.IService>();
                services.Add(tcpRelay);
                services.Add(udpRelay);
                services.Add(_pacServer);
                services.Add(new PortForwarder(privoxyRunner.RunningPort));
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
                        e = new Exception(I18N.GetString("Port already in use"), e);
                    }
                }
                Logging.LogUsefulException(e);
                ReportError(e);
            }

            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }

            UpdateSystemProxy();
            Utils.ReleaseMemory(true);
        }

        private void StartPlugins()
        {
            foreach (var server in _config.configs)
            {
                // Early start plugin processes
                GetPluginLocalEndPointIfConfigured(server);
            }
        }

        protected void SaveConfig(Configuration newConfig)
        {
            Configuration.Save(newConfig);
            Reload();
        }

        private void UpdateSystemProxy()
        {
            SystemProxy.Update(_config, false, _pacServer);
        }

        private void pacServer_PACFileChanged(object sender, EventArgs e)
        {
            UpdateSystemProxy();
        }

        private void pacServer_PACUpdateCompleted(object sender, GFWListUpdater.ResultEventArgs e)
        {
            if (UpdatePACFromGFWListCompleted != null)
                UpdatePACFromGFWListCompleted(this, e);
        }

        private void pacServer_PACUpdateError(object sender, ErrorEventArgs e)
        {
            if (UpdatePACFromGFWListError != null)
                UpdatePACFromGFWListError(this, e);
        }

        private static readonly IEnumerable<char> IgnoredLineBegins = new[] { '!', '[' };
        private void pacServer_UserRuleFileChanged(object sender, EventArgs e)
        {
            // TODO: this is a dirty hack. (from code GListUpdater.http_DownloadStringCompleted())
            if (!File.Exists(Utils.GetTempPath("gfwlist.txt")))
            {
                UpdatePACFromGFWList();
                return;
            }
            List<string> lines = new List<string>();
            if (File.Exists(PACServer.USER_RULE_FILE))
            {
                string local = FileManager.NonExclusiveReadAllText(PACServer.USER_RULE_FILE, Encoding.UTF8);
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
            lines.AddRange(GFWListUpdater.ParseResult(FileManager.NonExclusiveReadAllText(Utils.GetTempPath("gfwlist.txt"))));
            string abpContent;
            if (File.Exists(PACServer.USER_ABP_FILE))
            {
                abpContent = FileManager.NonExclusiveReadAllText(PACServer.USER_ABP_FILE, Encoding.UTF8);
            }
            else
            {
                abpContent = Utils.UnGzip(Resources.abp_js);
            }
            abpContent = abpContent.Replace("__RULES__", JsonConvert.SerializeObject(lines, Formatting.Indented));
            if (File.Exists(PACServer.PAC_FILE))
            {
                string original = FileManager.NonExclusiveReadAllText(PACServer.PAC_FILE, Encoding.UTF8);
                if (original == abpContent)
                {
                    return;
                }
            }
            File.WriteAllText(PACServer.PAC_FILE, abpContent, Encoding.UTF8);
        }

        public void CopyPacUrl()
        {
            Clipboard.SetDataObject(_pacServer.PacUrl);
        }

        #region Memory Management

        private void StartReleasingMemory()
        {
            _ramThread = new Thread(new ThreadStart(ReleaseMemory));
            _ramThread.IsBackground = true;
            _ramThread.Start();
        }

        private void ReleaseMemory()
        {
            while (true)
            {
                Utils.ReleaseMemory(false);
                Thread.Sleep(30 * 1000);
            }
        }

        #endregion

        #region Traffic Statistics

        private void StartTrafficStatistics(int queueMaxSize)
        {
            trafficPerSecondQueue = new Queue<TrafficPerSecond>();
            for (int i = 0; i < queueMaxSize; i++)
            {
                trafficPerSecondQueue.Enqueue(new TrafficPerSecond());
            }
            _trafficThread = new Thread(new ThreadStart(() => TrafficStatistics(queueMaxSize)));
            _trafficThread.IsBackground = true;
            _trafficThread.Start();
        }

        private void TrafficStatistics(int queueMaxSize)
        {
            TrafficPerSecond previous, current;
            while (true)
            {
                previous = trafficPerSecondQueue.Last();
                current = new TrafficPerSecond();
                
                current.inboundCounter = InboundCounter;
                current.outboundCounter = OutboundCounter;
                current.inboundIncreasement = current.inboundCounter - previous.inboundCounter;
                current.outboundIncreasement = current.outboundCounter - previous.outboundCounter;

                trafficPerSecondQueue.Enqueue(current);
                if (trafficPerSecondQueue.Count > queueMaxSize)
                    trafficPerSecondQueue.Dequeue();

                TrafficChanged?.Invoke(this, new EventArgs());

                Thread.Sleep(1000);
            }
        }

        #endregion

    }
}
