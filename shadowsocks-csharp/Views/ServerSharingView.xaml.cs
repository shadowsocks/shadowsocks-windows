using Shadowsocks.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shadowsocks.Views
{
    /// <summary>
    /// Interaction logic for ServerSharingView.xaml
    /// </summary>
    public partial class ServerSharingView : UserControl
    {
        public ServerSharingView()
        {
            InitializeComponent();

            DataContext = new ServerSharingViewModel();
        }

        private void urlTextBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            urlTextBox.SelectAll();
        }
    }
}
