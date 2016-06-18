using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Shadowsocks.Models;

namespace Shadowsocks
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Controller controller;
        public MainWindow()
        {
            InitializeComponent();
            var load = Configuration.Load();
            if (load == null)
                throw new Exception("put a config file here or add a new server");
            controller = new Controller(load);
            label.Content = $"You have {controller.servers.Length} servers,\nCurrent server is {controller.currentServer}\nLocalPort is {controller.localPort}";
            button1.Content = "Start";
            button2.Content = "DownloadPAC";
        }

        private async void button1_Click(object sender, RoutedEventArgs e)
        {
            button1.IsEnabled = false;
            if ((string) button1.Content == "Start")
            {
                await controller.StartAsync().ConfigureAwait(true);
                button1.Content = "Stop";
            }
            else
            {
                await controller.StopAsync().ConfigureAwait(true);
                button1.Content = "Start";
            }
            button1.IsEnabled = true;
        }

        private async void button2_Click(object sender, RoutedEventArgs e)
        {
            button2.IsEnabled = false;
            await controller.UpdateGFWListAsync().ConfigureAwait(true);
            button2.IsEnabled = true;
        }
    }
}
