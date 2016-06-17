using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Shadowsocks.Models
{
    public interface IConfig
    {
        List<Server> servers { get; set; }
        string currentServer { get; set; }
        bool global { get; set; }
        bool enabled { get; set; }
        bool shareOverLan { get; set; }
        bool isDefault { get; set; }
        int localPort { get; set; }
        string pacUrl { get; set; }
        bool useOnlinePac { get; set; }

        int releaseMemoryPeriod { get; set; }

        void Save();
    }
}
