using ReactiveUI;
using Shadowsocks.WPF.ViewModels;
using System.Reactive.Disposables;

namespace Shadowsocks.WPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel();
            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.GetDashboardView,
                    view => view.dashboardTabItem.Content)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.GetServersView,
                    view => view.serversTabItem.Content)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.GetRoutingView,
                    view => view.routingTabItem.Content)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.GetSettingsView,
                    view => view.settingsTabItem.Content)
                    .DisposeWith(disposables);
            });
        }
    }
}
