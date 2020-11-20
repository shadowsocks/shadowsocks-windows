namespace Shadowsocks.WPF.Models
{
    public class Group : Shadowsocks.Models.Group
    {
        /// <summary>
        /// Gets or sets the URL of SIP008 online configuration delivery source.
        /// </summary>
        public string OnlineConfigSource { get; set; }

        public Group() : this(string.Empty)
        { }

        public Group(string name) : base(name)
        {
            OnlineConfigSource = "";
        }
    }
}
