using shadowsocks_csharp.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace shadowsocks_csharp.Controller
{
    public class ShadowsocksController
    {
        // controller:
        // handle user actions
        // manipulates UI
        // interacts with low level logic

        private Local local;
        private PACServer pacServer;
        private Config config;
        private PolipoRunner polipoRunner;
        private bool stopped = false;

        public class PathEventArgs : EventArgs
        {
            public string Path;
        }

        public class ErrorEventArgs : EventArgs
        {
            public string Error;
        }

        public event EventHandler ConfigChanged;
        public event EventHandler EnableStatusChanged;

        public event EventHandler<ErrorEventArgs> LocalFailToStart;
        
        // when user clicked Edit PAC, and PAC file has already created
        public event EventHandler<PathEventArgs> PACFileReadyToOpen;

        public ShadowsocksController()
        {
            config = Config.Load();
            polipoRunner = new PolipoRunner();
            polipoRunner.Start(config);
            local = new Local(config);
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

            updateSystemProxy();
        }

        public void SaveConfig(Config newConfig)
        {
            Config.Save(newConfig);
            config = newConfig;

            local.Stop();
            polipoRunner.Stop();
            polipoRunner.Start(config);

            local = new Local(config);
            local.Start();

            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }
        }

        public Config GetConfig()
        {
            return config;
        }

        public void ToggleEnable(bool enabled)
        {
            config.enabled = enabled;
            updateSystemProxy();
            SaveConfig(config);
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
            if (config.enabled)
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

        private void updateSystemProxy()
        {
            if (config.enabled)
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
            updateSystemProxy();
        }

    }
}
