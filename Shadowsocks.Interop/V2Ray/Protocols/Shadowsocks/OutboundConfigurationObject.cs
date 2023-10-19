using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Protocols.Shadowsocks;

public class OutboundConfigurationObject
{
    public List<ServerObject> Servers { get; set; }

    public OutboundConfigurationObject() => Servers = [];

    public OutboundConfigurationObject(string address, int port, string method, string password)
    => Servers = [new ServerObject(address, port, method, password)];
}