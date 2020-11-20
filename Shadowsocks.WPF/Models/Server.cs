namespace Shadowsocks.WPF.Models
{
    public class Server : Shadowsocks.Models.Server
    {
        /// <summary>
        /// Gets or sets the arguments passed to the plugin process.
        /// </summary>
        public string PluginArgs { get; set; }

        public Server()
        {
            PluginArgs = "";
        }

        public Server(
            string name,
            string uuid,
            string host,
            int port,
            string password,
            string method,
            string plugin = "",
            string pluginOpts = "",
            string pluginArgs = "")
            : base(name, uuid, host, port, password, method, plugin, pluginOpts)
        {
            PluginArgs = pluginArgs;
        }
    }
}
