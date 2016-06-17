using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace Shadowsocks.Models
{
    [Serializable]
    public class Configuration : IConfig
    {
        public List<Server> servers { get; set; } = new List<Server>();
        public string currentServer { get; set; }
        public bool global { get; set; }
        public bool enabled { get; set; }
        public bool shareOverLan { get; set; }
        public bool isDefault { get; set; }
        public int localPort { get; set; }
        public string pacUrl { get; set; }
        public bool useOnlinePac { get; set; }
        public int releaseMemoryPeriod { get; set; }

        public void AddServer(IEnumerable<Server> svcs, bool Override = false)
        {
            foreach (var server in svcs)
            {
                if (servers.Contains(server) && Override)
                    servers.Remove(server);
                servers.Add(server);
            }
        }

        public static Configuration Load()
        {
            Configuration ret = null;
            if (File.Exists(Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + ".json"))
                ret = Utils.LoadConfig<Configuration>(Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + ".json");
            if (File.Exists("ssmod.json"))
            {
                if (ret == null)
                {
                    ret = Utils.LoadConfig<Configuration>("ssmod.json");
                }
                else
                {
                    ret.AddServer(Utils.LoadConfig<Configuration>("ssmod.json").servers);
                }
            }
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ssmod.json"))
            {
                if (ret == null)
                {
                    ret = Utils.LoadConfig<Configuration>(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ssmod.json");
                }
                else
                {
                    ret.AddServer(Utils.LoadConfig<Configuration>(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ssmod.json").servers);
                }
            }
            if (File.Exists(OfficialConfig.CONFIG_FILE))
            {
                var oconfig = OfficialConfig.Load();
                if (ret == null)
                {
                    ret = new Configuration
                    {
                        global = oconfig.global,
                        enabled = oconfig.enabled,
                        shareOverLan = oconfig.shareOverLan,
                        isDefault = oconfig.isDefault,
                        localPort = oconfig.localPort,
                        pacUrl = oconfig.pacUrl,
                        useOnlinePac = oconfig.useOnlinePac,
                        releaseMemoryPeriod = 30,
                        servers = new List<Server>()
                    };
                    ret.AddServer(oconfig.configs);
                    ret.currentServer = ret.servers.FirstOrDefault()?.Identifier;
                }
                else
                {
                    ret.AddServer(oconfig.configs);
                }
            }
            return ret ?? new Configuration();
        }

        public void Save()
        {
            try
            {
                this.SaveConfig(Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + ".json");
            }
            catch (SecurityException)
            {
                this.SaveConfig(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ssmod.json");
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                throw;
            }
        }
    }
}
