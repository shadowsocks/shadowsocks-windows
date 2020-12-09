namespace Shadowsocks.Interop.V2Ray.Transport
{
    public class StreamSettingsObject : TransportObject
    {
        /// <summary>
        /// Gets or sets the transport protocol type.
        /// Defaults to "tcp".
        /// Available values: "tcp" | "kcp" | "ws" | "http" | "domainsocket" | "quic"
        /// </summary>
        public string? Network { get; set; }

        /// <summary>
        /// Gets or sets the transport encryption type.
        /// Defaults to "none" (no encryption).
        /// Available values: "none" | "tls"
        /// </summary>
        public string? Security { get; set; }

        public TlsObject? TlsSettings { get; set; }
        public SockoptObject? Sockopt { get; set; }

        public static StreamSettingsObject DefaultWsTls => new()
        {
            Network = "ws",
            Security = "tls",
            TlsSettings = new(),
        };
    }
}
