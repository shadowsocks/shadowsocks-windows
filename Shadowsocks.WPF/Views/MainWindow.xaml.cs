using ReactiveUI;

using Shadowsocks.WPF.ViewModels;

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

            });
        }
    }
}
