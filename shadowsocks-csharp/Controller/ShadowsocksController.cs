using System.IO;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Sockets;

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
        private PolipoRunner polipoRunner;
        private GFWListUpdater gfwListUpdater;
        private bool stopped = false;

        private bool _systemProxyIsDirty = false;

        public class PathEventArgs : EventArgs
        {
            public string Path;
        }

        public event EventHandler ConfigChanged;
        public event EventHandler EnableStatusChanged;
        public event EventHandler EnableGlobalChanged;
        //public event EventHandler ShareOverLANStatusChanged;
        public event EventHandler SelectRandomStatusChanged;
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
        public Configuration GetConfiguration()
        {
            return Configuration.Load();
        }

        public Configuration GetCurrentConfiguration()
        {
            return _config;
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
                            && mergeConfig.configs[i].method == servers[j].method
                            && mergeConfig.configs[i].protocol == servers[j].protocol
                            && mergeConfig.configs[i].obfs == servers[j].obfs
                            && mergeConfig.configs[i].password == servers[j].password
                            && mergeConfig.configs[i].tcp_over_udp == servers[j].tcp_over_udp
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
                        && mergeConfig.configs[i].method == servers[j].method
                        && mergeConfig.configs[i].protocol == servers[j].protocol
                        && mergeConfig.configs[i].obfs == servers[j].obfs
                        && mergeConfig.configs[i].password == servers[j].password
                        && mergeConfig.configs[i].tcp_over_udp == servers[j].tcp_over_udp
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

        public void SaveServersConfig(Configuration config)
        {
            List<Server> missingServers = MergeConfiguration(_config, config.configs);
            _config.configs = config.configs;
            _config.index = config.index;
            _config.buildinHttpProxy = config.buildinHttpProxy;
            _config.shareOverLan = config.shareOverLan;
            _config.localPort = config.localPort;
            _config.reconnectTimes = config.reconnectTimes;
            _config.random = config.random;
            _config.randomAlgorithm = config.randomAlgorithm;
            _config.TTL = config.TTL;
            _config.socks5enable = config.socks5enable;
            _config.socks5Host = config.socks5Host;
            _config.socks5Port = config.socks5Port;
            _config.socks5User = config.socks5User;
            _config.socks5Pass = config.socks5Pass;
            _config.autoban = config.autoban;
            SaveConfig(_config);
            foreach (Server s in missingServers)
            {
                s.GetConnections().CloseAll();
            }
            SelectServerIndex(_config.index);
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

        //public void ToggleShareOverLAN(bool enabled)
        //{
        //    _config.shareOverLan = enabled;
        //    SaveConfig(_config);
        //    if (ShareOverLANStatusChanged != null)
        //    {
        //        ShareOverLANStatusChanged(this, new EventArgs());
        //    }
        //}

        public void ToggleSelectRandom(bool enabled)
        {
            _config.random = enabled;
            SaveConfig(_config);
            if (SelectRandomStatusChanged != null)
            {
                SelectRandomStatusChanged(this, new EventArgs());
            }
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

        protected string GetObfsPartOfSSLink(Server server)
        {
            string parts = "";
            if (server.protocol.Length > 0 && server.protocol != "origin")
            {
                parts = server.protocol + ":" + parts;
            }
            if (server.obfs.Length > 0 && server.obfs != "plain")
            {
                parts = server.obfs + ":" + parts;
            }
            parts = parts + server.method + ":" + server.password + "@" + server.server + ":" + server.server_port;
            if (server.obfs.Length > 0 && server.obfs != "plain" && server.obfsparam.Length > 0)
            {
                parts += "/" + System.Convert.ToBase64String(Encoding.UTF8.GetBytes(server.obfsparam));
            }
            return parts;
        }

        public string GetSSLinkForCurrentServer()
        {
            Server server = GetCurrentServer();
            string parts = GetObfsPartOfSSLink(server);
            string base64 = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(parts));
            return "ss://" + base64;
        }

        public string GetSSLinkForServer(Server server)
        {
            string parts = GetObfsPartOfSSLink(server);
            string base64 = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(parts));
            return "ss://" + base64;
        }

        public string GetSSRemarksLinkForServer(Server server)
        {
            string remarks = server.remarks_base64;
            string parts = GetObfsPartOfSSLink(server) + "#" + remarks;
            string base64 = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(parts));
            return "ss://" + base64;
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

        protected void Reload()
        {
            // some logic in configuration updated the config when saving, we need to read it again
            _config = MergeGetConfiguration(_config);

            if (polipoRunner == null)
            {
                polipoRunner = new PolipoRunner();
            }
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
            try
            {
                if (_listener != null && !_listener.isConfigChange(_config))
                {
                    Local local = new Local(_config);
                    _listener.GetServices()[0] = local;
                    if (polipoRunner.HasExited())
                    {
                        polipoRunner.Stop();
                        polipoRunner.Start(_config);
                    }
                }
                else
                {
                    if (_listener != null)
                    {
                        _listener.Stop();
                        _listener = null;
                    }

                    polipoRunner.Stop();
                    polipoRunner.Start(_config);

                    Local local = new Local(_config);
                    List<Listener.Service> services = new List<Listener.Service>();
                    services.Add(local);
                    services.Add(_pacServer);
                    if (!_config.buildinHttpProxy)
                    {
                        services.Add(new PortForwarder(polipoRunner.RunningPort));
                    }
                    _listener = new Listener(services);
                    _listener.Start(_config);
                }
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
            Util.Utils.ReleaseMemory();
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

        public void ShowConfigForm()
        {
            if (ShowConfigFormEvent != null)
            {
                ShowConfigFormEvent(this, new EventArgs());
            }
        }
    }
}
