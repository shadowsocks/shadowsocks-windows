using ReactiveUI;
using Shadowsocks.WPF.Views;

namespace Shadowsocks.WPF.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public DashboardView GetDashboardView { get; } = new();

    public ServersView GetServersView { get; } = new();

    public RoutingView GetRoutingView { get; } = new();

    public SettingsView GetSettingsView { get; } = new();
}