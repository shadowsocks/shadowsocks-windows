using ReactiveUI;
using Shadowsocks.WPF.ViewModels;
using System.Reactive.Disposables;

namespace Shadowsocks.WPF.Views
{
    /// <summary>
    /// Interaction logic for OnlineConfigView.xaml
    /// </summary>
    public partial class OnlineConfigView
    {
        public OnlineConfigView()
        {
            InitializeComponent();
            ViewModel = new OnlineConfigViewModel();
            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Sources,
                    view => view.sourcesListBox.ItemsSource)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
                    viewModel => viewModel.SelectedSource,
                    view => view.sourcesListBox.SelectedItem)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
                    viewModel => viewModel.Address,
                    view => view.urlTextBox.Text)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.Update,
                    view => view.updateButton)
                    .DisposeWith(disposables);
                this.BindCommand(ViewModel,
                    viewModel => viewModel.UpdateAll,
                    view => view.updateAllButton)
                    .DisposeWith(disposables);
                this.BindCommand(ViewModel,
                    viewModel => viewModel.CopyLink,
                    view => view.copyLinkButton)
                    .DisposeWith(disposables);
                this.BindCommand(ViewModel,
                    viewModel => viewModel.Remove,
                    view => view.removeButton)
                    .DisposeWith(disposables);
                this.BindCommand(ViewModel,
                    viewModel => viewModel.Add,
                    view => view.addButton)
                    .DisposeWith(disposables);
            });
        }
    }
}
