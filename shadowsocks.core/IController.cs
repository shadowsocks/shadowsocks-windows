using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shadowsocks.Models;

namespace Shadowsocks
{
    public interface IController
    {
        event System.IO.ErrorEventHandler Errored;

        Server[] servers { get; }
        string currentServer { get; }
        bool global { get; }
        bool enabled { get; }
        bool shareOverLan { get; }
        int localPort { get; }
        string pacUrl { get; }
        bool useOnlinePac { get; }
        int releaseMemoryPeriod { get; }

        Server GetCurrentServer();

        Task StartAsync();
        Task StopAsync();
        Task ApplyConfigAsync(IConfig newConfig);
        Task SaveServerAsync(IEnumerable<Server> svc);
        Task SelectServerAsync(string serverIdentity);
        Task SelectServerAsync(Server server);
        Task ToggleEnableAsync(bool isEnabled);
        Task ToggleGlobalAsync(bool isGlobal);
        Task ToggleShareOverLANAsync(bool isEnabled);
        Task SavePACUrlAsync(string Url);
        Task UseOnlinePACAsync(bool isUseOnlinePac);
        Task ChangeLocalPortAsync(int newPort);
        void ChangeReleaseMemoryPeriod(int newPeriodTime);

        Task<string> TouchPACFileAsync();
        Task<string> TouchUserRuleFileAsync();
        bool AddUserRule(string line);

        Task UpdateGFWListAsync(System.Net.IWebProxy proxy = null);
    }
}
