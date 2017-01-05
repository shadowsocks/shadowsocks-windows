using System.IO;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace Shadowsocks.Controller
{
    public enum ProxyMode
    {
        NoModify,
        Pac,
        Global,
    }

    public class ShadowsocksController
    {
        // controller:
        // handle user actions
        // manipulates UI
        // interacts with low level logic

        private Thread _ramThread;

        private Listener _listener;
        private List<Listener> _port_map_listener;
        private PACServer _pacServer;
        private Configuration _config;
        private ServerTransferTotal _transfer;
        public IPRangeSet _rangeSet;
#if !_CONSOLE
        private HttpProxyRunner polipoRunner;
        private bool _systemProxyIsDirty = false;
#endif
        private GFWListUpdater gfwListUpdater;
        private bool stopped = false;
        private bool firstRun = true;


        public class PathEventArgs : EventArgs
        {
            public string Path;
        }

        public event EventHandler ConfigChanged;
        public event EventHandler ToggleModeChanged;
        public event EventHandler ToggleRuleModeChanged;
        //public event EventHandler ShareOverLANStatusChanged;
        public event EventHandler ShowConfigFormEvent;

        // when user clicked Edit PAC, and PAC file has already created
        public event EventHandler<PathEventArgs> PACFileReadyToOpen;
        public event EventHandler<PathEventArgs> UserRuleFileReadyToOpen;

        public event EventHandler<GFWListUpdater.ResultEventArgs> UpdatePACFromGFWListCompleted;

        public event ErrorEventHandler UpdatePACFromGFWListError;

        public event ErrorEventHandler Errored;

        public ShadowsocksController()
        {
            _config = Configuration.Load();
            _transfer = ServerTransferTotal.Load();

            foreach (Server server in _config.configs)
            {
                if (_transfer.servers.ContainsKey(server.server))
                {
                    ServerSpeedLog log = new ServerSpeedLog(((ServerTrans)_transfer.servers[server.server]).totalUploadBytes, ((ServerTrans)_transfer.servers[server.server]).totalDownloadBytes);
                    server.SetServerSpeedLog(log);
                }
            }
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

        public void ReloadIPRange()
        {
            _rangeSet = new IPRangeSet();
            _rangeSet.LoadApnic("CN");
            if (_config.proxyRuleMode == 3)
            {
                _rangeSet.Reverse();
            }
        }

        public Server GetCurrentServer()
        {
            return _config.GetCurrentServer();
        }

        // always return copy
        public Configuration GetConfiguration()
        {
            return Configuration.Load();
        }

        public Configuration GetCurrentConfiguration()
        {
            return _config;
        }

        public void AppendConfiguration(Configuration mergeConfig, List<Server> servers)
        {
            if (servers != null)
            {
                for (int j = 0; j < servers.Count; ++j)
                {
                    int i = 0;
                    for (; i < mergeConfig.configs.Count; ++i)
                    {
                        if (mergeConfig.configs[i].server == servers[j].server
                            && mergeConfig.configs[i].server_port == servers[j].server_port
                            && mergeConfig.configs[i].server_udp_port == servers[j].server_udp_port
                            && mergeConfig.configs[i].method == servers[j].method
                            && mergeConfig.configs[i].protocol == servers[j].protocol
                            && mergeConfig.configs[i].protocolparam == servers[j].protocolparam
                            && mergeConfig.configs[i].obfs == servers[j].obfs
                            && mergeConfig.configs[i].obfsparam == servers[j].obfsparam
                            && mergeConfig.configs[i].password == servers[j].password
                            && mergeConfig.configs[i].udp_over_tcp == servers[j].udp_over_tcp
                            )
                        {
                            break;
                        }
                    }
                    if (i == mergeConfig.configs.Count)
                    {
                        mergeConfig.configs.Add(servers[j]);
                    }
                }
            }
        }

        public List<Server> MergeConfiguration(Configuration mergeConfig, List<Server> servers)
        {
            List<Server> missingServers = new List<Server>();
            if (servers != null)
            {
                for (int j = 0; j < servers.Count; ++j)
                {
                    for (int i = 0; i < mergeConfig.configs.Count; ++i)
                    {
                        if (mergeConfig.configs[i].server == servers[j].server
                            && mergeConfig.configs[i].server_port == servers[j].server_port
                            && mergeConfig.configs[i].server_udp_port == servers[j].server_udp_port
                            && mergeConfig.configs[i].method == servers[j].method
                            && mergeConfig.configs[i].protocol == servers[j].protocol
                            && mergeConfig.configs[i].protocolparam == servers[j].protocolparam
                            && mergeConfig.configs[i].obfs == servers[j].obfs
                            && mergeConfig.configs[i].obfsparam == servers[j].obfsparam
                            && mergeConfig.configs[i].password == servers[j].password
                            && mergeConfig.configs[i].udp_over_tcp == servers[j].udp_over_tcp
                            )
                        {
                            servers[j].CopyServer(mergeConfig.configs[i]);
                            break;
                        }
                    }
                }
            }
            for (int i = 0; i < mergeConfig.configs.Count; ++i)
            {
                int j = 0;
                for (; j < servers.Count; ++j)
                {
                    if (mergeConfig.configs[i].server == servers[j].server
                        && mergeConfig.configs[i].server_port == servers[j].server_port
                        && mergeConfig.configs[i].server_udp_port == servers[j].server_udp_port
                        && mergeConfig.configs[i].method == servers[j].method
                        && mergeConfig.configs[i].protocol == servers[j].protocol
                        && mergeConfig.configs[i].protocolparam == servers[j].protocolparam
                        && mergeConfig.configs[i].obfs == servers[j].obfs
                        && mergeConfig.configs[i].obfsparam == servers[j].obfsparam
                        && mergeConfig.configs[i].password == servers[j].password
                        && mergeConfig.configs[i].udp_over_tcp == servers[j].udp_over_tcp
                        )
                    {
                        break;
                    }
                }
                if (j == servers.Count)
                {
                    missingServers.Add(mergeConfig.configs[i]);
                }
            }
            return missingServers;
        }

        public Configuration MergeGetConfiguration(Configuration mergeConfig)
        {
            Configuration ret = Configuration.Load();
            if (mergeConfig != null)
            {
                MergeConfiguration(mergeConfig, ret.configs);
            }
            return ret;
        }

        public void MergeConfiguration(Configuration mergeConfig)
        {
            AppendConfiguration(_config, mergeConfig.configs);
            SaveConfig(_config);
        }

        public void SaveServers(List<Server> servers, int localPort)
        {
            List<Server> missingServers = MergeConfiguration(_config, servers);
            _config.configs = servers;
            _config.localPort = localPort;
            SaveConfig(_config);
            foreach(Server s in missingServers)
            {
                s.GetConnections().CloseAll();
            }
        }

        public bool SaveServersConfig(string config)
        {
            Configuration new_cfg = Configuration.Load(config);
            if (new_cfg != null)
            {
                SaveServersConfig(new_cfg);
                return true;
            }
            return false;
        }

        public void SaveServersConfig(Configuration config)
        {
            List<Server> missingServers = MergeConfiguration(_config, config.configs);
            _config.configs = config.configs;
            _config.index = config.index;
            _config.random = config.random;
            _config.sysProxyMode = config.sysProxyMode;
            _config.shareOverLan = config.shareOverLan;
            _config.bypassWhiteList = config.bypassWhiteList;
            _config.localPort = config.localPort;
            _config.reconnectTimes = config.reconnectTimes;
            _config.randomAlgorithm = config.randomAlgorithm;
            _config.TTL = config.TTL;
            _config.connect_timeout = config.connect_timeout;
            _config.dns_server = config.dns_server;
            _config.proxyEnable = config.proxyEnable;
            _config.pacDirectGoProxy = config.pacDirectGoProxy;
            _config.proxyType = config.proxyType;
            _config.proxyHost = config.proxyHost;
            _config.proxyPort = config.proxyPort;
            _config.proxyAuthUser = config.proxyAuthUser;
            _config.proxyAuthPass = config.proxyAuthPass;
            _config.proxyUserAgent = config.proxyUserAgent;
            _config.authUser = config.authUser;
            _config.authPass = config.authPass;
            _config.autoBan = config.autoBan;
            _config.sameHostForSameTarget = config.sameHostForSameTarget;
            _config.keepVisitTime = config.keepVisitTime;
            _config.isHideTips = config.isHideTips;
            foreach (Server s in missingServers)
            {
                s.GetConnections().CloseAll();
            }
            SelectServerIndex(_config.index);
        }

        public void SaveServersPortMap(Configuration config)
        {
            _config.portMap = config.portMap;
            SelectServerIndex(_config.index);
            _config.FlushPortMapCache();
        }

        public bool AddServerBySSURL(string ssURL)
        {
            if (ssURL.StartsWith("ss://", StringComparison.OrdinalIgnoreCase) || ssURL.StartsWith("ssr://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var server = new Server(ssURL);
                    int index = _config.index + 1;
                    if (index < 0 || index > _config.configs.Count)
                        index = _config.configs.Count;
                    _config.configs.Insert(index, server);
                    SaveConfig(_config);
                    return true;
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void ToggleMode(int mode)
        {
            _config.sysProxyMode = mode;
            SaveConfig(_config);
            if (ToggleModeChanged != null)
            {
                ToggleModeChanged(this, new EventArgs());
            }
        }

        public void ToggleRuleMode(int mode)
        {
            _config.proxyRuleMode = mode;
            SaveConfig(_config);
            if (ToggleRuleModeChanged != null)
            {
                ToggleRuleModeChanged(this, new EventArgs());
            }
        }

        public void ToggleBypass(bool bypass)
        {
            _config.bypassWhiteList = bypass;
            SaveConfig(_config);
        }

        public void ToggleSelectRandom(bool enabled)
        {
            _config.random = enabled;
            SaveConfig(_config);
        }

        public void ToggleSameHostForSameTargetRandom(bool enabled)
        {
            _config.sameHostForSameTarget = enabled;
            SaveConfig(_config);
        }

        public void SelectServerIndex(int index)
        {
            _config.index = index;
            SaveConfig(_config);
        }

        public void Stop()
        {
            if (stopped)
            {
                return;
            }
            stopped = true;

            if (_port_map_listener != null)
            {
                foreach (Listener l in _port_map_listener)
                {
                    l.Stop();
                }
                _port_map_listener = null;
            }
            if (_listener != null)
            {
                _listener.Stop();
            }
#if !_CONSOLE
            if (polipoRunner != null)
            {
                polipoRunner.Stop();
            }
            if (_config.sysProxyMode != (int)ProxyMode.NoModify)
            {
                SystemProxy.Update(_config, true);
            }
#endif
            ServerTransferTotal.Save(_transfer);
        }

        public void ClearTransferTotal(string server_addr)
        {
            _transfer.Clear(server_addr);
            foreach (Server server in _config.configs)
            {
                if (server.server == server_addr)
                {
                    if (_transfer.servers.ContainsKey(server.server))
                    {
                        server.ServerSpeedLog().ClearTrans();
                    }
                }
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

        public void UpdatePACFromGFWList()
        {
            if (gfwListUpdater != null)
            {
                gfwListUpdater.UpdatePACFromGFWList(_config);
            }
        }

        public void UpdatePACFromOnlinePac(string url)
        {
            if (gfwListUpdater != null)
            {
                gfwListUpdater.UpdatePACFromGFWList(_config, url);
            }
        }

        public void UpdateBypassListFromDefault()
        {
            if (gfwListUpdater != null)
            {
                gfwListUpdater.UpdateBypassListFromDefault(_config);
            }
        }

        public void SaveLogViewerConfig(LogViewerConfig config)
        {
            _config.logViewer = config;
            Configuration.Save(_config);

            ConfigChanged?.Invoke(this, new EventArgs());
        }

        protected void Reload()
        {
            if (_port_map_listener != null)
            {
                foreach (Listener l in _port_map_listener)
                {
                    l.Stop();
                }
                _port_map_listener = null;
            }
            // some logic in configuration updated the config when saving, we need to read it again
            _config = MergeGetConfiguration(_config);
            _config.FlushPortMapCache();
            ReloadIPRange();

#if !_CONSOLE
            if (polipoRunner == null)
            {
                polipoRunner = new HttpProxyRunner();
            }
#endif
            if (_pacServer == null)
            {
                _pacServer = new PACServer();
                _pacServer.PACFileChanged += pacServer_PACFileChanged;
            }
            _pacServer.UpdateConfiguration(_config);
            if (gfwListUpdater == null)
            {
                gfwListUpdater = new GFWListUpdater();
                gfwListUpdater.UpdateCompleted += pacServer_PACUpdateCompleted;
                gfwListUpdater.Error += pacServer_PACUpdateError;
            }

            // don't put polipoRunner.Start() before pacServer.Stop()
            // or bind will fail when switching bind address from 0.0.0.0 to 127.0.0.1
            // though UseShellExecute is set to true now
            // http://stackoverflow.com/questions/10235093/socket-doesnt-close-after-application-exits-if-a-launched-process-is-open
            bool _firstRun = firstRun;
            for (int i = 1; i <= 5; ++i)
            {
                _firstRun = false;
                try
                {
                    if (_listener != null && !_listener.isConfigChange(_config))
                    {
                        Local local = new Local(_config, _transfer, _rangeSet);
                        _listener.GetServices()[0] = local;
#if !_CONSOLE
                        if (polipoRunner.HasExited())
                        {
                            polipoRunner.Stop();
                            polipoRunner.Start(_config);

                            _listener.GetServices()[3] = new HttpPortForwarder(polipoRunner.RunningPort, _config);
                        }
#endif
                    }
                    else
                    {
                        if (_listener != null)
                        {
                            _listener.Stop();
                            _listener = null;
                        }

#if !_CONSOLE
                        polipoRunner.Stop();
                        polipoRunner.Start(_config);
#endif

                        Local local = new Local(_config, _transfer, _rangeSet);
                        List<Listener.Service> services = new List<Listener.Service>();
                        services.Add(local);
                        services.Add(_pacServer);
                        services.Add(new APIServer(this, _config));
#if !_CONSOLE
                        services.Add(new HttpPortForwarder(polipoRunner.RunningPort, _config));
#endif
                        _listener = new Listener(services);
                        _listener.Start(_config, 0);
                    }
                    break;
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
                            e = new Exception(I18N.GetString("Port already in use") + string.Format(" {0}", _config.localPort), e);
                        }
                    }
                    Logging.LogUsefulException(e);
                    if (!_firstRun)
                    {
                        ReportError(e);
                        break;
                    }
                    else
                    {
                        Thread.Sleep(1000 * i * i);
                    }
                    if (_listener != null)
                    {
                        _listener.Stop();
                        _listener = null;
                    }
                }
            }

            _port_map_listener = new List<Listener>();
            foreach (KeyValuePair<int, PortMapConfigCache> pair in _config.GetPortMapCache())
            {
                try
                {
                    Local local = new Local(_config, _transfer, _rangeSet);
                    List<Listener.Service> services = new List<Listener.Service>();
                    services.Add(local);
                    Listener listener = new Listener(services);
                    listener.Start(_config, pair.Key);
                    _port_map_listener.Add(listener);
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
                            e = new Exception(I18N.GetString("Port already in use") + string.Format(" {0}", pair.Key), e);
                        }
                    }
                    Logging.LogUsefulException(e);
                    ReportError(e);
                }
            }

            ConfigChanged?.Invoke(this, new EventArgs());

            UpdateSystemProxy();
            Util.Utils.ReleaseMemory();
        }


        protected void SaveConfig(Configuration newConfig)
        {
            Configuration.Save(newConfig);
            Reload();
        }


        private void UpdateSystemProxy()
        {
#if !_CONSOLE
            if (_config.sysProxyMode != (int)ProxyMode.NoModify)
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
#endif
        }

        private void pacServer_PACFileChanged(object sender, EventArgs e)
        {
            UpdateSystemProxy();
        }

        private void pacServer_PACUpdateCompleted(object sender, GFWListUpdater.ResultEventArgs e)
        {
            if (UpdatePACFromGFWListCompleted != null)
                UpdatePACFromGFWListCompleted(sender, e);
        }

        private void pacServer_PACUpdateError(object sender, ErrorEventArgs e)
        {
            if (UpdatePACFromGFWListError != null)
                UpdatePACFromGFWListError(sender, e);
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
                Util.Utils.ReleaseMemory();
                Thread.Sleep(30 * 1000);
            }
        }

        public void ShowConfigForm(int index)
        {
            if (ShowConfigFormEvent != null)
            {
                ShowConfigFormEvent(index, new EventArgs());
            }
        }
    }
}
