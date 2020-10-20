using ReactiveUI;
using Shadowsocks.ViewModels;
using System.Reactive.Disposables;
using System.Windows.Input;

namespace Shadowsocks.Views
{
    /// <summary>
    /// Interaction logic for ServerSharingView.xaml
    /// </summary>
    public partial class ServerSharingView : ReactiveUserControl<ServerSharingViewModel>
    {
        public ServerSharingView()
        {
            InitializeComponent();
            ViewModel = new ServerSharingViewModel();
            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.SelectedServerUrlImage,
                    view => view.qrCodeImage.Source)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Servers,
                    view => view.serversListBox.ItemsSource)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
                    viewModel => viewModel.SelectedServer,
                    view => view.serversListBox.SelectedItem)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.SelectedServerUrl,
                    view => view.urlTextBox.Text)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.CopyLink,
                    view => view.copyLinkButton)
                    .DisposeWith(disposables);
            });
        }

        private void urlTextBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            urlTextBox.SelectAll();
        }
    }
}
