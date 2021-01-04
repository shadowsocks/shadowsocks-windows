using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shadowsocks.WPF.Views;

namespace Shadowsocks.WPF.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public MainWindowViewModel()
        {
            GetDashboardView = new();
            GetServersView = new();
            GetRoutingView = new();
            GetSettingsView = new();
        }

        public DashboardView GetDashboardView { get; }

        public ServersView GetServersView { get; }

        public RoutingView GetRoutingView { get; }

        public SettingsView GetSettingsView { get; }
    }
}
