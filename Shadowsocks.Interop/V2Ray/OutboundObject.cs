using Shadowsocks.Interop.V2Ray.Outbound;
using Shadowsocks.Interop.V2Ray.Transport;
using Shadowsocks.Models;
using System;
using System.Net;

namespace Shadowsocks.Interop.V2Ray
{
    public class OutboundObject
    {
        public string Tag { get; set; }
        public string? SendThrough { get; set; }
        public string Protocol { get; set; }
        public object? Settings { get; set; }
        public StreamSettingsObject? StreamSettings { get; set; }
        public ProxySettingsObject? ProxySettings { get; set; }
        public MuxObject? Mux { get; set; }

        public OutboundObject()
        {
            Tag = "";
            Protocol = "";
        }

        /// <summary>
        /// Gets the <see cref="OutboundObject"/> for the SOCKS server.
        /// </summary>
        /// <param name="name">SOCKS server name. Used as outbound tag.</param>
        /// <param name="socksEndPoint">The SOCKS server.</param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static OutboundObject GetSocks(string name, DnsEndPoint socksEndPoint, string username = "", string password = "") => new()
        {
            Tag = name,
            Protocol = "socks",
            Settings = new Protocols.Socks.OutboundConfigurationObject(socksEndPoint, username, password),
        };

        /// <summary>
        /// Gets the <see cref="OutboundObject"/> for the Shadowsocks server.
        /// Plugins are not supported.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static OutboundObject GetShadowsocks(IServer server)
        {
            if (!string.IsNullOrEmpty(server.Plugin))
                throw new InvalidOperationException("V2Ray doesn't support SIP003 plugins.");
            
            return new()
            {
                Tag = server.Name,
                Protocol = "shadowsocks",
                Settings = new Protocols.Shadowsocks.OutboundConfigurationObject(server.Host, server.Port, server.Method, server.Password),
            };
        }

        /// <summary>
        /// Gets the <see cref="OutboundObject"/> for the Trojan server.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static OutboundObject GetTrojan(string name, string address, int port, string password) => new()
        {
            Tag = name,
            Protocol = "trojan",
            Settings = new Protocols.Trojan.OutboundConfigurationObject(address, port, password),
        };

        /// <summary>
        /// Gets the <see cref="OutboundObject"/> for the VMess server.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static OutboundObject GetVMess(string name, string address, int port, string id) => new()
        {
            Tag = name,
            Protocol = "vmess",
            Settings = new Protocols.VMess.OutboundConfigurationObject(address, port, id),
        };
    }
}
