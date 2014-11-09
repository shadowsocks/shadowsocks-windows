using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Text;

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
        
        // when user clicked Edit PAC, and PAC file has already created
        public event EventHandler<PathEventArgs> PACFileReadyToOpen;

        public ShadowsocksController()
        {
            _config = Configuration.Load();
            polipoRunner = new PolipoRunner();
            polipoRunner.Start(_config.GetCurrentServer());
            local = new Local(_config.GetCurrentServer());
            try
            {
                local.Start();
                pacServer = new PACServer();
                pacServer.PACFileChanged += pacServer_PACFileChanged;
                pacServer.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            UpdateSystemProxy();
        }

        public void SaveConfig(Configuration newConfig)
        {
            Configuration.Save(newConfig);
            // some logic in configuration updated the config when saving, we need to read it again
            _config = Configuration.Load();

            local.Stop();
            polipoRunner.Stop();
            polipoRunner.Start(_config.GetCurrentServer());

            local = new Local(_config.GetCurrentServer());
            local.Start();

            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
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

    }
}
