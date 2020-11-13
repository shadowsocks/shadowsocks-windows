using Shadowsocks.Net.SystemProxy;
using Shadowsocks.PAC;
using Shadowsocks.WPF.Models;
using Shadowsocks.WPF.Services.SystemProxy;
using Splat;
using System.Windows;

namespace Shadowsocks.WPF.Utils
{
    public static class SystemProxy
    {
        public static void Update(bool forceDisable, bool enabled, bool global)
        {
            var settings = Locator.Current.GetService<Settings>();
            var appSettings = settings.App;
            var netSettings = settings.Net;
            var pacSettings = settings.PAC;

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
                        WinINet.ProxyGlobal($"localhost:{netSettings.HttpListeningPort}", "<local>");
                    }
                    else
                    {
                        var pacUrl = string.IsNullOrEmpty(pacSettings.CustomPACUrl)
                            ? Locator.Current.GetService<PACServer>().PacUrl : pacSettings.CustomPACUrl;
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
                LogHost.Default.Error(ex, "An error occurred while updating system proxy.");
                /*if (ex.Type != ProxyExceptionType.Unspecific && !noRetry)
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
                }*/
            }
        }
    }
}