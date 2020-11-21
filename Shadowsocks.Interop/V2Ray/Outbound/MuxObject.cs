namespace Shadowsocks.Interop.V2Ray.Outbound
{
    public class MuxObject
    {
        /// <summary>
        /// Gets or sets whether to enable mux.
        /// Defaults to false (disabled).
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the concurrency for a single TCP connection when using mux.
        /// Defaults to 8.
        /// Range: [1, 1024].
        /// Set to -1 to disable the mux module.
        /// </summary>
        public int Concurrency { get; set; }

        public MuxObject()
        {
            Enabled = false;
            Concurrency = 8;
        }
    }
}
