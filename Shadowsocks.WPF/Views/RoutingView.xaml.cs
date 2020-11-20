using ReactiveUI;
using Shadowsocks.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Shadowsocks.WPF.Views
{
    /// <summary>
    /// Interaction logic for RoutingView.xaml
    /// </summary>
    public partial class RoutingView
    {
        public RoutingView()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {

            });
        }
    }
}
