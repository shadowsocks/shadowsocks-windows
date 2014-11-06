using System;
using System.Collections.Generic;
using System.Text;

namespace shadowsocks_csharp
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

        public event EventHandler ConfigChanged;
        public event EventHandler EnableStatusChanged;

        public ShadowsocksController()
        {
            config = Config.Load();
            polipoRunner = new PolipoRunner();
            polipoRunner.Start(config);
            local = new Local(config);
            local.Start();
            pacServer = new PACServer();
            pacServer.Start();

            updateSystemProxy();
        }

        public void SaveConfig(Config newConfig)
        {
            Config.Save(newConfig);
            config = newConfig;
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
    }
}
