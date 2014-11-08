﻿using System.IO;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Shadowsocks.Controller
{
    public class ShadowsocksController
    {
        // controller:
        // handle user actions
        // manipulates UI
        // interacts with low level logic

        private Local local;
        private PACServer pacServer;
        private Configuration _config;
        private PolipoRunner polipoRunner;
        private bool stopped = false;

        public class PathEventArgs : EventArgs
        {
            public string Path;
        }

        public event EventHandler ConfigChanged;
        public event EventHandler EnableStatusChanged;
        public event EventHandler ShareOverLANStatusChanged;
        
        // when user clicked Edit PAC, and PAC file has already created
        public event EventHandler<PathEventArgs> PACFileReadyToOpen;

        public ShadowsocksController()
        {
            _config = Configuration.Load();
            polipoRunner = new PolipoRunner();
            polipoRunner.Start(_config);
            local = new Local(_config);
            try
            {
                local.Start();
                pacServer = new PACServer();
                pacServer.PACFileChanged += pacServer_PACFileChanged;
                pacServer.Start(_config);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            UpdateSystemProxy();
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

        public void SaveServers(List<Server> servers, bool noChange)
        {
            _config.configs = servers;
            _config.noChange = noChange;
            SaveConfig(_config);
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

        public void ToggleShareOverLAN(bool enabled)
        {
            _config.shareOverLan = enabled;
            _config.noChange = false;
            SaveConfig(_config);
            if (ShareOverLANStatusChanged != null)
            {
                ShareOverLANStatusChanged(this, new EventArgs());
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
            local.Stop();
            polipoRunner.Stop();
            if (_config.enabled)
            {
                SystemProxy.Disable();
            }
        }

        public void TouchPACFile()
        {
            string pacFilename = pacServer.TouchPACFile();
            if (PACFileReadyToOpen != null)
            {
                PACFileReadyToOpen(this, new PathEventArgs() { Path = pacFilename });
            }
        }

        public string GetQRCodeForCurrentServer()
        {
            Server server = GetCurrentServer();
            string parts = server.method + ":" + server.password + "@" + server.server + ":" + server.server_port;
            string base64 = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(parts));
            return "ss://" + base64;
        }


        public void SaveConfig(Configuration newConfig)
        {
            Configuration.Save(newConfig);
            if (newConfig.noChange)
            {
                return;
            }
            // some logic in configuration updated the config when saving, we need to read it again
            _config = Configuration.Load();

            pacServer.Stop();
            local.Stop();

            // don't put polipoRunner.Start() before pacServer.Stop()
            // or bind will fail when switching bind address from 0.0.0.0 to 127.0.0.1
            // though UseShellExecute is set to true now
            // http://stackoverflow.com/questions/10235093/socket-doesnt-close-after-application-exits-if-a-launched-process-is-open
            polipoRunner.Stop();
            polipoRunner.Start(_config);

            local = new Local(_config);
            local.Start();
            pacServer.Start(_config);

            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }
        }


        private void UpdateSystemProxy()
        {
            if (_config.enabled)
            {
                SystemProxy.Enable();
            }
            else
            {
                SystemProxy.Disable();
            }
        }

        private void pacServer_PACFileChanged(object sender, EventArgs e)
        {
            UpdateSystemProxy();
        }

        private void SetLog()
        {
                try
                {
                    FileStream fs = new FileStream("shadowsocks.log", FileMode.Append);
                    TextWriter tmp = Console.Out;
                    StreamWriter sw = new StreamWriter(fs);
                    sw.AutoFlush = true;
                    Console.SetOut(sw);
                    Console.SetError(sw);
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.ToString());
                }
        }

    }
}
