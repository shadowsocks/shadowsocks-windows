using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using Newtonsoft.Json;

using Shadowsocks.Controller.Strategy;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    public class ShadowsocksController
    {
        // controller:
        // handle user actions
        // manipulates UI
        // interacts with low level logic

        private Thread _ramThread;

        private Listener _listener;
        private PACServer _pacServer;
        private Configuration _config;
        private StrategyManager _strategyManager;
        private PolipoRunner polipoRunner;
        private GFWListUpdater gfwListUpdater;
        public AvailabilityStatistics availabilityStatistics { get; private set; }
        public StatisticsStrategyConfiguration StatisticsConfiguration { get; private set; }

        public long inboundCounter = 0;
        public long outboundCounter = 0;

        private bool stopped = false;

        private bool _systemProxyIsDirty = false;

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

        public event EventHandler<GFWListUpdater.ResultEventArgs> UpdatePACFromGFWListCompleted;

        public event ErrorEventHandler UpdatePACFromGFWListError;

        public event ErrorEventHandler Errored;

        public ShadowsocksController()
        {
            _config = Configuration.Load();
            StatisticsConfiguration = StatisticsStrategyConfiguration.Load();
            _strategyManager = new StrategyManager(this);
            StartReleasingMemory();
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

        public Server GetAServer(IStrategyCallerType type, IPEndPoint localIPEndPoint)
        {
            IStrategy strategy = GetCurrentStrategy();
            if (strategy != null)
            {
                return strategy.GetAServer(type, localIPEndPoint);
            }
            if (_config.index < 0)
            {
                _config.index = 0;
            }
            return GetCurrentServer();
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
            if (EnableStatusChanged != null)
            {
                EnableStatusChanged(this, new EventArgs());
            }
        }

        public void ToggleGlobal(bool global)
        {
            _config.global = global;
            UpdateSystemProxy();
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
            if (polipoRunner != null)
            {
                polipoRunner.Stop();
            }
            if (_config.enabled)
            {
                SystemProxy.Update(_config, true);
            }
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

        public string GetQRCodeForCurrentServer()
        {
            Server server = GetCurrentServer();
            return GetQRCode(server);
        }

        public static string GetQRCode(Server server)
        {
            string parts = server.method + ":" + server.password + "@" + server.server + ":" + server.server_port;
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(parts));
            return "ss://" + base64;
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
            availabilityStatistics.UpdateConfiguration(_config, StatisticsConfiguration);
            _config.availabilityStatistics = enabled;
            SaveConfig(_config);
        }

        public void SavePACUrl(string pacUrl)
        {
            _config.pacUrl = pacUrl;
            UpdateSystemProxy();
            SaveConfig(_config);
            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }
        }

        public void UseOnlinePAC(bool useOnlinePac)
        {
            _config.useOnlinePac = useOnlinePac;
            UpdateSystemProxy();
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
        }

        public void SaveLogViewerConfig(LogViewerConfig newConfig)
        {
            _config.logViewer = newConfig;
            Configuration.Save(_config);
        }

        public void UpdateInboundCounter(long n)
        {
            Interlocked.Add(ref inboundCounter, n);
        }

        public void UpdateOutboundCounter(long n)
        {
            Interlocked.Add(ref outboundCounter, n);
        }

        protected void Reload()
        {
            // some logic in configuration updated the config when saving, we need to read it again
            _config = Configuration.Load();
            StatisticsConfiguration = StatisticsStrategyConfiguration.Load();

            if (polipoRunner == null)
            {
                polipoRunner = new PolipoRunner();
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

            if (availabilityStatistics == null)
            {
                availabilityStatistics = new AvailabilityStatistics(_config, StatisticsConfiguration);
            }
            availabilityStatistics.UpdateConfiguration(_config, StatisticsConfiguration);

            if (_listener != null)
            {
                _listener.Stop();
            }
            // don't put polipoRunner.Start() before pacServer.Stop()
            // or bind will fail when switching bind address from 0.0.0.0 to 127.0.0.1
            // though UseShellExecute is set to true now
            // http://stackoverflow.com/questions/10235093/socket-doesnt-close-after-application-exits-if-a-launched-process-is-open
            polipoRunner.Stop();
            try
            {
                var strategy = GetCurrentStrategy();
                if (strategy != null)
                {
                    strategy.ReloadServers();
                }

                polipoRunner.Start(_config);

                TCPRelay tcpRelay = new TCPRelay(this);
                UDPRelay udpRelay = new UDPRelay(this);
                List<Listener.Service> services = new List<Listener.Service>();
                services.Add(tcpRelay);
                services.Add(udpRelay);
                services.Add(_pacServer);
                services.Add(new PortForwarder(polipoRunner.RunningPort));
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

        private void pacServer_UserRuleFileChanged(object sender, EventArgs e)
        {
            // TODO: this is a dirty hack. (from code GListUpdater.http_DownloadStringCompleted())
            if (!File.Exists(Utils.GetTempPath("gfwlist.txt")))
            {
                UpdatePACFromGFWList();
                return;
            }
            List<string> lines = GFWListUpdater.ParseResult(File.ReadAllText(Utils.GetTempPath("gfwlist.txt")));
            if (File.Exists(PACServer.USER_RULE_FILE))
            {
                string local = File.ReadAllText(PACServer.USER_RULE_FILE, Encoding.UTF8);
                string[] rules = local.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string rule in rules)
                {
                    if (rule[0] == '!' || rule[0] == '[')
                        continue;
                    lines.Add(rule);
                }
            }
            string abpContent;
            if (File.Exists(PACServer.USER_ABP_FILE))
            {
                abpContent = File.ReadAllText(PACServer.USER_ABP_FILE, Encoding.UTF8);
            }
            else
            {
                abpContent = Utils.UnGzip(Resources.abp_js);
            }
            abpContent = abpContent.Replace("__RULES__", JsonConvert.SerializeObject(lines, Formatting.Indented));
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
    }
}
