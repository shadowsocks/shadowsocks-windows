using ReactiveUI;
using Shadowsocks.ViewModels;
using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Shadowsocks.Views
{
    /// <summary>
    /// Interaction logic for HotkeysView.xaml
    /// </summary>
    public partial class HotkeysView : ReactiveUserControl<HotkeysViewModel>
    {
        public HotkeysView()
        {
            InitializeComponent();
            ViewModel = new HotkeysViewModel();
            this.WhenActivated(disposables =>
            {
                systemProxyTextBox
                    .Events().KeyDown
                    .Subscribe(keyEventArgs => ViewModel.RecordKeyDown(0, keyEventArgs))
                    .DisposeWith(disposables);

                systemProxyTextBox
                    .Events().KeyUp
                    .Subscribe(keyEventArgs => ViewModel.FinishOnKeyUp(0, keyEventArgs))
                    .DisposeWith(disposables);

                proxyModeTextBox
                    .Events().KeyDown
                    .Subscribe(keyEventArgs => ViewModel.RecordKeyDown(1, keyEventArgs))
                    .DisposeWith(disposables);

                proxyModeTextBox
                    .Events().KeyUp
                    .Subscribe(keyEventArgs => ViewModel.FinishOnKeyUp(1, keyEventArgs))
                    .DisposeWith(disposables);

                allowLanTextBox
                    .Events().KeyDown
                    .Subscribe(keyEventArgs => ViewModel.RecordKeyDown(2, keyEventArgs))
                    .DisposeWith(disposables);

                allowLanTextBox
                    .Events().KeyUp
                    .Subscribe(keyEventArgs => ViewModel.FinishOnKeyUp(2, keyEventArgs))
                    .DisposeWith(disposables);

                openLogsTextBox
                    .Events().KeyDown
                    .Subscribe(keyEventArgs => ViewModel.RecordKeyDown(3, keyEventArgs))
                    .DisposeWith(disposables);

                openLogsTextBox
                    .Events().KeyUp
                    .Subscribe(keyEventArgs => ViewModel.FinishOnKeyUp(3, keyEventArgs))
                    .DisposeWith(disposables);

                switchPrevTextBox
                    .Events().KeyDown
                    .Subscribe(keyEventArgs => ViewModel.RecordKeyDown(4, keyEventArgs))
                    .DisposeWith(disposables);

                switchPrevTextBox
                    .Events().KeyUp
                    .Subscribe(keyEventArgs => ViewModel.FinishOnKeyUp(4, keyEventArgs))
                    .DisposeWith(disposables);

                switchNextTextBox
                    .Events().KeyDown
                    .Subscribe(keyEventArgs => ViewModel.RecordKeyDown(5, keyEventArgs))
                    .DisposeWith(disposables);

                switchNextTextBox
                    .Events().KeyUp
                    .Subscribe(keyEventArgs => ViewModel.FinishOnKeyUp(5, keyEventArgs))
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.HotkeySystemProxy,
                    view => view.systemProxyTextBox.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.HotkeyProxyMode,
                    view => view.proxyModeTextBox.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.HotkeyAllowLan,
                    view => view.allowLanTextBox.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.HotkeyOpenLogs,
                    view => view.openLogsTextBox.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.HotkeySwitchPrev,
                    view => view.switchPrevTextBox.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.HotkeySwitchNext,
                    view => view.switchNextTextBox.Text)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.RegisterAtStartup,
                    view => view.registerAtStartupCheckBox.IsChecked)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.HotkeySystemProxyStatus,
                    view => view.systemProxyStatusTextBlock.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.HotkeyProxyModeStatus,
                    view => view.proxyModeStatusTextBlock.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.HotkeyAllowLanStatus,
                    view => view.allowLanStatusTextBlock.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.HotkeyOpenLogsStatus,
                    view => view.openLogsStatusTextBlock.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.HotkeySwitchPrevStatus,
                    view => view.switchPrevStatusTextBlock.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.HotkeySwitchNextStatus,
                    view => view.switchNextStatusTextBlock.Text)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.RegisterAll,
                    view => view.registerAllButton)
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
