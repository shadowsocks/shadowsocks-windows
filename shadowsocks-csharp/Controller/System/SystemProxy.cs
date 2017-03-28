using System;
using Shadowsocks.Model;
using Shadowsocks.Util.SystemProxy;

namespace Shadowsocks.Controller
{
    public static class SystemProxy
    {
        private static bool _shouldRecord = true;

        private static string GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssfff");
        }

        public static void Update(Configuration config, bool forceDisable, PACServer pacSrv)
        {
            bool global = config.global;
            bool enabled = config.enabled;

            if (forceDisable)
            {
                enabled = false;
            }

            try
            {
                if (enabled)
                {
                    // Should record only once after enabled.
                    if (_shouldRecord)
                    {
                        UserProxy.Record();
                        _shouldRecord = false;
                    }

                    if (global)
                    {
                        Sysproxy.SetIEProxy(true, true, "127.0.0.1:" + config.localPort.ToString(), "");
                    }
                    else
                    {
                        string pacUrl;
                        if (config.useOnlinePac && !config.pacUrl.IsNullOrEmpty())
                        {
                            pacUrl = config.pacUrl;
                        }
                        else
                        {
                            pacUrl = pacSrv.PacUrl;
                        }
                        Sysproxy.SetIEProxy(true, false, "", pacUrl);
                    }
                }
                else
                {
                    UserProxy.Restore();
                    _shouldRecord = true;
                }
            }
            catch (ProxyException ex)
            {
                Logging.LogUsefulException(ex);
            }
        }
    }
}