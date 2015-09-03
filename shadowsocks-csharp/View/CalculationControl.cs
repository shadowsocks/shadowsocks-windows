using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Shadowsocks.View
{
    public partial class CalculationControl : UserControl
    {
        public CalculationControl(string text, float value)
        {
            InitializeComponent();
            valueLabel.Text = text;
            factorNum.Value = (decimal) value;
        }

        public string Value => valueLabel.Text;
        public float Factor => (float) factorNum.Value;
    }
}
