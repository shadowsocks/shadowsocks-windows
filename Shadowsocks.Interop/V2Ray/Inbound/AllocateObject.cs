namespace Shadowsocks.Interop.V2Ray.Inbound
{
    public class AllocateObject
    {
        /// <summary>
        /// Gets or sets the port allocation strategy.
        /// Defaults to "always".
        /// Available values: "always" | "random"
        /// </summary>
        public string Strategy { get; set; }

        /// <summary>
        /// Gets or sets the random port refreshing interval in minutes.
        /// Defaults to 5 minutes.
        /// </summary>
        public int? Refresh { get; set; }

        /// <summary>
        /// Gets or sets the number of random ports.
        /// Defaults to 3.
        /// </summary>
        public int? Concurrency { get; set; }

        public AllocateObject()
        {
            Strategy = "always";
        }

        public static AllocateObject Default => new()
        {
            Refresh = 5,
            Concurrency = 3,
        };
    }
}
