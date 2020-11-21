namespace Shadowsocks.Interop.V2Ray.Transport
{
    public class DomainSocketObject
    {
        /// <summary>
        /// Gets or sets the path to the unix domain socket file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets whether the domain socket is abstract.
        /// Defaults to false.
        /// </summary>
        public bool Abstract { get; set; }

        /// <summary>
        /// Gets or sets whether padding is used.
        /// Defaults to false.
        /// </summary>
        public bool Padding { get; set; }

        public DomainSocketObject()
        {
            Path = "";
            Abstract = false;
            Padding = false;
        }
    }
}
