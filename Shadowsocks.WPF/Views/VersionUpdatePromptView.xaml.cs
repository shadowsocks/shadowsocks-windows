using ReactiveUI;
using Shadowsocks.WPF.ViewModels;
using System.Reactive.Disposables;
using System.Text.Json;

namespace Shadowsocks.WPF.Views
{
    /// <summary>
    /// Interaction logic for VersionUpdatePromptView.xaml
    /// </summary>
    public partial class VersionUpdatePromptView
    {
        public VersionUpdatePromptView()
        {
            InitializeComponent();
            DataContext = ViewModel; // for compatibility with MdXaml
            this.WhenActivated(disposables =>
            {
                /*this.OneWayBind(ViewModel,
                    viewModel => viewModel.ReleaseNotes,
                    view => releaseNotesMarkdownScrollViewer.Markdown)
                    .DisposeWith(disposables);*/

                this.BindCommand(ViewModel!,
                    viewModel => viewModel.Update,
                    view => view.updateButton)
                    .DisposeWith(disposables);
                this.BindCommand(ViewModel!,
                    viewModel => viewModel.SkipVersion,
                    view => view.skipVersionButton)
                    .DisposeWith(disposables);
                this.BindCommand(ViewModel!,
                    viewModel => viewModel.NotNow,
                    view => view.notNowButton)
                    .DisposeWith(disposables);
            });
        }
    }
}
