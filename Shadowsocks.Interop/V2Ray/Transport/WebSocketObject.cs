using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Transport
{
    public class WebSocketObject
    {
        /// <summary>
        /// Gets or sets whether to use PROXY protocol.
        /// </summary>
        public bool AcceptProxyProtocol { get; set; }

        /// <summary>
        /// Gets or sets the HTTP query path.
        /// Defaults to "/".
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets HTTP header key-value pairs.
        /// Defaults to empty.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        public WebSocketObject()
        {
            AcceptProxyProtocol = false;
            Path = "/";
            Headers = new();
        }
    }
}
