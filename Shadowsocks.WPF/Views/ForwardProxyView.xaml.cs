using ReactiveUI;
using Shadowsocks.WPF.ViewModels;
using System.Reactive.Disposables;

namespace Shadowsocks.WPF.Views
{
    /// <summary>
    /// Interaction logic for ForwardProxyView.xaml
    /// </summary>
    public partial class ForwardProxyView
    {
        public ForwardProxyView()
        {
            InitializeComponent();
            ViewModel = new ForwardProxyViewModel();
            this.WhenActivated(disposables =>
            {
                this.Bind(ViewModel,
                    viewModel => viewModel.NoProxy,
                    view => view.noProxyRadioButton.IsChecked)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
                    viewModel => viewModel.UseSocks5Proxy,
                    view => view.socks5RadioButton.IsChecked)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
                    viewModel => viewModel.UseHttpProxy,
                    view => view.httpRadioButton.IsChecked)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Address,
                    view => view.addressTextBox.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
                    viewModel => viewModel.Port,
                    view => view.portTextBox.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
                    viewModel => viewModel.Timeout,
                    view => view.timeoutTextBox.Text)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.CanModifyDetails,
                    view => view.addressTextBox.IsEnabled)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.CanModifyDetails,
                    view => view.portTextBox.IsEnabled)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.CanModifyDetails,
                    view => view.timeoutTextBox.IsEnabled)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Username,
                    view => view.usernameTextBox.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
                    viewModel => viewModel.Password,
                    view => view.passwordTextBox.Text)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.UseHttpProxy,
                    view => view.usernameTextBox.IsEnabled)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.UseHttpProxy,
                    view => view.passwordTextBox.IsEnabled)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.Save,
                    view => view.saveButton)
                    .DisposeWith(disposables);
                this.BindCommand(ViewModel,
                    viewModel => viewModel.Cancel,
                    view => view.cancelButton)
                    .DisposeWith(disposables);
            });
        }
    }
}
