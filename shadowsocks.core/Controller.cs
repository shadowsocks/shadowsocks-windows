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
    public class Controller:IController
    {
        #region Fields
        private Listener _listener;
        private readonly PACServer _pacServer;
        private readonly PrivoxyRunner _privoxyRunner;

        private bool stopped;
        private readonly Server _defaultServer = new Server();

        private bool _systemProxyIsBusy;

        private System.Threading.Timer _tmrReleaseMemory;

        private bool _analysis;
        private readonly Dictionary<Server,long> inboundCounter = new Dictionary<Server, long>();
        private readonly Dictionary<Server, long> outboundCounter = new Dictionary<Server, long>();
        private readonly Dictionary<Server, List<TimeSpan>> latencyCounter = new Dictionary<Server, List<TimeSpan>>();
        #endregion

        #region Properties
        private IConfig _config;
        //shake it baby!
        public Server[] servers => _config.servers.ToArray();
        public string currentServer => _config.currentServer;
        public bool global => _config.global;
        public bool enabled => _config.enabled;
        public bool shareOverLan => _config.shareOverLan;
        public int localPort => _config.localPort;
        public string pacUrl => _config.pacUrl;
        public bool useOnlinePac => _config.useOnlinePac;
        public int releaseMemoryPeriod => _config.releaseMemoryPeriod;
        #endregion

        #region Events
        private void pacServer_PACFileChanged(object sender, EventArgs e)
        {
            UpdateSystemProxy();
        }
        private async void pacServer_UserRuleFileChanged(object sender, EventArgs e)
        {
            if (!File.Exists(Utils.GetTempPath("gfwlist.txt")))
            {
                await UpdateGFWListAsync().ConfigureAwait(false);
                return;
            }
            PushGFWList(File.ReadAllText(Utils.GetTempPath("gfwlist.txt")));
        }
        public event ErrorEventHandler Errored;
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
                _systemProxyIsBusy = true;
            }
            else
            {
                // only switch it off if we have switched it on
                if (!_systemProxyIsBusy) return;
                SystemProxy.Update(_config, false);
                _systemProxyIsBusy = false;
            }
        }
        private void StartReleasingMemory()
        {
            _tmrReleaseMemory?.Dispose();
            _tmrReleaseMemory = new System.Threading.Timer(ReleaseMemory, null, 0, (_config.releaseMemoryPeriod < 1 ? 30 : _config.releaseMemoryPeriod) * 1000);
        }

        protected Task ReloadAsync()
        {
            return Task.Run(() =>
            {
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
                    var tcpRelay = new TCPRelay(this);
                    var udpRelay = new UDPRelay(this);
                    var services = new List<Listener.Service> {tcpRelay, udpRelay, _pacServer, new PortForwarder(_privoxyRunner.RunningPort)};
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
                    Errored?.Invoke(this, new ErrorEventArgs(e));
                }
                UpdateSystemProxy();
                Utils.ReleaseMemory(true);
            });
        }
        #endregion

        #region PublicMethods
        public Server GetCurrentServer()
        {
            return _config.GetCurrentServer();
        }

        public async Task StartAsync()
        {
            if(_config.GetCurrentServer().Equals(_defaultServer))
                throw new Exception("Please add some server!");
            await ReloadAsync().ConfigureAwait(false);
        }

        public Task StopAsync()
        {
            return Task.Run(() =>
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
            });
        }

        public async Task ApplyConfigAsync(IConfig newConfig)
        {
            _config = newConfig;
            await ReloadAsync().ConfigureAwait(false);
        }

        public Task SaveServerAsync(IEnumerable<Server> svc)
        {
            return Task.Run(() =>
            {
                _config.servers = svc.ToList();
                _config.Save();
            });
        }

        public async Task SelectServerAsync(string serverIdentity)
        {
            if (_config.servers.All(c => c.Identifier != serverIdentity)) throw new Exception("Server not found");
            _config.currentServer = serverIdentity;
            await ReloadAsync().ConfigureAwait(false);
        }

        public async Task SelectServerAsync(Server server)
        {
            _config.currentServer = server.Identifier;
            await ReloadAsync().ConfigureAwait(false);
        }

        public async Task ToggleEnableAsync(bool isEnabled)
        {
            _config.enabled = isEnabled;
            UpdateSystemProxy();
            _config.Save();
            await ReloadAsync().ConfigureAwait(false);
        }

        public async Task ToggleGlobalAsync(bool isGlobal)
        {
            _config.global = global;
            UpdateSystemProxy();
            _config.Save();
            await ReloadAsync().ConfigureAwait(false);
        }

        public async Task ToggleShareOverLANAsync(bool isEnabled)
        {
            _config.shareOverLan = enabled;
            _config.Save();
            await ReloadAsync().ConfigureAwait(false);
        }

        public async Task SavePACUrlAsync(string Url)
        {
            _config.pacUrl = Url;
            UpdateSystemProxy();
            _config.Save();
            await ReloadAsync().ConfigureAwait(false);
        }

        public async Task UseOnlinePACAsync(bool isUseOnlinePac)
        {
            _config.useOnlinePac = isUseOnlinePac;
            UpdateSystemProxy();
            _config.Save();
            await ReloadAsync().ConfigureAwait(false);
        }

        public async Task ChangeLocalPortAsync(int newPort)
        {
            _config.localPort = newPort;
            UpdateSystemProxy();
            _config.Save();
            await ReloadAsync().ConfigureAwait(false);
        }

        public void ChangeReleaseMemoryPeriod(int newPeriodTime)
        {
            _config.releaseMemoryPeriod = newPeriodTime;
            _config.Save();
            StartReleasingMemory();
        }


        public Task<string> TouchPACFileAsync()
        {
            return Task.Run(() => _pacServer.TouchPACFile());
        }

        public Task<string> TouchUserRuleFileAsync()
        {
            return Task.Run(() => _pacServer.TouchUserRuleFile());
        }

        public bool AddUserRule(string line)
        {
            return _pacServer.AddLine(line);
        }
        #endregion

        #region GFWList
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
