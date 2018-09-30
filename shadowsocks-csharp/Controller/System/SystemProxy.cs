using System;
using System.Windows.Forms;
using Shadowsocks.Model;
using Shadowsocks.Util.SystemProxy;

namespace Shadowsocks.Controller
{
    public static class SystemProxy
    {
        private static bool failed = false;

        private static string GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssfff");
        }

        public static void Update(Configuration config, bool forceDisable, PACServer pacSrv)
        {
            if (failed) return;
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
                    if (global)
                    {
                        Sysproxy.SetIEProxy(true, true, "127.0.0.1:" + config.localPort.ToString(), null);
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
                        Sysproxy.SetIEProxy(true, false, null, pacUrl);
                    }
                }
                else
                {
                    Sysproxy.SetIEProxy(false, false, null, null);
                }
            }
            catch (ProxyException ex)
            {
                Logging.LogUsefulException(ex);
                switch (ex.Type)
                {
                    case ProxyExceptionType.FailToRun:
                        MessageBox.Show(I18N.GetString("Error when running sysproxy, check your proxy config"), I18N.GetString("Shadowsocks"));
                        break;
                    case ProxyExceptionType.QueryReturnMalformed:
                    case ProxyExceptionType.QueryReturnEmpty:
                        MessageBox.Show(I18N.GetString("Can't query proxy config, check your proxy config"), I18N.GetString("Shadowsocks"));
                        break;
                    case ProxyExceptionType.SysproxyExitError:
                        MessageBox.Show(I18N.GetString("Sysproxy return a error:") + ex.Message, I18N.GetString("Shadowsocks"));
                        break;
                }
            }
        }
    }
}