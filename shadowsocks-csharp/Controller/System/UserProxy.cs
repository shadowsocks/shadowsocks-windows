using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shadowsocks.Util;
using Shadowsocks.Util.SystemProxy;

namespace Shadowsocks.Controller
{
    /// <summary>
    /// Record and restore user's proxy settings.
    /// </summary>
    public static class UserProxy
    {
        private static ProxyValues _proxyValues;

        public static void Record()
        {
            // read ProxyOverride, ProxyEnable, ProxyServer, AutoConfigURL from Software\Microsoft\Windows\CurrentVersion\Internet Settings and save
            using (var regKey = Utils.OpenRegKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", false))
            {
                if (regKey == null)
                {
                    return;
                }
                _proxyValues = new ProxyValues
                {
                    GlobalProxyEnable = (int) regKey.GetValue("ProxyEnable", 0),
                    GlobalProxyOverride = (string) regKey.GetValue("ProxyOverride"),
                    GlobalProxyServer = (string) regKey.GetValue("ProxyServer"),
                    PACAutoConfigURL = (string) regKey.GetValue("AutoConfigURL")
                };
                using (var connKey = regKey.OpenSubKey("Connections"))
                {
                    // this value saved all proxy values as byte[] including not using "Automatically detect settings"
                    // see http://stackoverflow.com/questions/1674119/what-key-in-windows-registry-disables-ie-connection-parameter-automatically-det
                    // "sysproxy.exe off" will enable "Automatically detect settings"
                    // try to restore all user's settings by writing this to registry directly, may not the best method
                    _proxyValues.DefaultConnectionSettings = (byte[]) connKey?.GetValue("DefaultConnectionSettings");
                }
            }
        }

        public static void Restore()
        {
            if (_proxyValues == null)
            {
                return;
            }
            try
            {
                if (!_proxyValues.GlobalProxyServer.IsNullOrEmpty())
                {
                    var arguments = $"global {_proxyValues.GlobalProxyServer} ";
                    if (!_proxyValues.GlobalProxyOverride.IsNullOrEmpty())
                    {
                        arguments += _proxyValues.GlobalProxyOverride;
                    }

                    // set user's global proxy even it's not enabled to make sure values are restored
                    Sysproxy.SetIEProxyWithArguments(arguments);
                }

                // setting pac will disable global proxy but leave values
                if (!_proxyValues.PACAutoConfigURL.IsNullOrEmpty())
                {
                    Sysproxy.SetIEProxy(true, false, string.Empty, _proxyValues.PACAutoConfigURL);
                }
                // off if neither pac nor global proxy are enabled
                else if (_proxyValues.GlobalProxyEnable == 0)
                {
                    Sysproxy.SetIEProxy(false, false, string.Empty, string.Empty);
                }
            }
            catch (ProxyException e)
            {
                Logging.LogUsefulException(e);
            }
            try
            {
                // for those who don't need "Automatically detect settings", may override things have done above
                using (var regKey = Utils.OpenRegKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections", true))
                {
                    regKey?.SetValue("DefaultConnectionSettings", _proxyValues.DefaultConnectionSettings);
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private class ProxyValues
        {
            public int GlobalProxyEnable { get; set; }
            public string GlobalProxyOverride { get; set; }
            public string GlobalProxyServer { get; set; }
            public string PACAutoConfigURL { get; set; }
            public byte[] DefaultConnectionSettings { get; set; }
        }
    }
}