using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Shadowsocks.View
{
    public partial class CalculationControl : UserControl
    {
        public CalculationControl(string value)
        {
            InitializeComponent();
            valueLabel.Text = value;
        }

        public string Value => valueLabel.Text;
        public float Factor => float.Parse(factorNum.Text);
    }
}
