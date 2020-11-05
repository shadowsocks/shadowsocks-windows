using NLog;
using Shadowsocks.Net.SystemProxy;
using Shadowsocks.WPF.Services.SystemProxy;
using System.Windows;

namespace Shadowsocks.WPF.Behaviors
{
    public static class SystemProxy
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static void Update(Configuration config, bool forceDisable, PACServer pacSrv, bool noRetry = false)
        {
            bool global = config.global;
            bool enabled = config.enabled;

            if (forceDisable || !WinINet.operational)
            {
                enabled = false;
            }

            try
            {
                if (enabled)
                {
                    if (global)
                    {
                        WinINet.ProxyGlobal("localhost:" + config.localPort.ToString(), "<local>");
                    }
                    else
                    {
                        string pacUrl;
                        if (config.useOnlinePac && !string.IsNullOrEmpty(config.pacUrl))
                        {
                            pacUrl = config.pacUrl;
                        }
                        else
                        {

                            pacUrl = pacSrv.PacUrl;
                        }
                        WinINet.ProxyPAC(pacUrl);
                    }
                }
                else
                {
                    WinINet.Restore();
                }
            }
            catch (ProxyException ex)
            {
                logger.LogUsefulException(ex);
                if (ex.Type != ProxyExceptionType.Unspecific && !noRetry)
                {
                    var ret = MessageBox.Show(I18N.GetString("Error occured when process proxy setting, do you want reset current setting and retry?"), I18N.GetString("Shadowsocks"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (ret == DialogResult.Yes)
                    {
                        WinINet.Reset();
                        Update(config, forceDisable, pacSrv, true);
                    }
                }
                else
                {
                    MessageBox.Show(I18N.GetString("Unrecoverable proxy setting error occured, see log for detail"), I18N.GetString("Shadowsocks"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}