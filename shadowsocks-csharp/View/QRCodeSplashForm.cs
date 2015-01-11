using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Shadowsocks.View
{
    public partial class QRCodeSplashForm : Form
    {
        public QRCodeSplashForm()
        {
            InitializeComponent();
        }

        private Timer timer;
        private int step;

        private void QRCodeSplashForm_Load(object sender, EventArgs e)
        {
            step = 0;
            timer = new Timer();
            timer.Interval = 300;
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            timer.Interval = 40;
            if (step == 0)
            {
                this.Opacity = 0;
            }
            else if (step == 1)
            {
                this.Opacity = 0.3;
            }
            else if (step == 1)
            {
                this.Opacity = 0.0;
            }
            else if (step == 2)
            {
                this.Opacity = 0.3;
            }
            else if (step == 3)
            {
                this.Opacity = 0.0;
            }
            else if (step == 4)
            {
                this.Opacity = 0.3;
            }
            else
            {
                this.Close();
            }
            step++;
        }
    }
}
