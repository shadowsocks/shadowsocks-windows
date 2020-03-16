using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Shadowsocks.View
{
    internal partial class InputBox : Form
    {
        public InputBox()
        {
            InitializeComponent();
        }

        internal string Prompt { get { return label1.Text; } set { label1.Text = value; } }
        internal string Response { get { return textBox1.Text; } set { textBox1.Text = value; } }

    }
}
