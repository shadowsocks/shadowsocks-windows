using Newtonsoft.Json.Linq;
using ReactiveUI;
using Shadowsocks.ViewModels;
using System.Reactive.Disposables;

namespace Shadowsocks.Views
{
    /// <summary>
    /// Interaction logic for VersionUpdatePromptView.xaml
    /// </summary>
    public partial class VersionUpdatePromptView : ReactiveUserControl<VersionUpdatePromptViewModel>
    {
        public VersionUpdatePromptView(JToken releaseObject)
        {
            InitializeComponent();
            ViewModel = new VersionUpdatePromptViewModel(releaseObject);
            DataContext = ViewModel; // for compatibility with MdXaml
            this.WhenActivated(disposables =>
            {
                /*this.OneWayBind(ViewModel,
                    viewModel => viewModel.ReleaseNotes,
                    view => releaseNotesMarkdownScrollViewer.Markdown)
                    .DisposeWith(disposables);*/

                this.BindCommand(ViewModel,
                    viewModel => viewModel.Update,
                    view => view.updateButton)
                    .DisposeWith(disposables);
                this.BindCommand(ViewModel,
                    viewModel => viewModel.SkipVersion,
                    view => view.skipVersionButton)
                    .DisposeWith(disposables);
                this.BindCommand(ViewModel,
                    viewModel => viewModel.NotNow,
                    view => view.notNowButton)
                    .DisposeWith(disposables);
            });
        }
    }
}
