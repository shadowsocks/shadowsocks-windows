using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Shadowsocks.View {
    public partial class HotkeySetting : Form {
        ShadowsocksController controller;
        // TODO: not finished
        public HotkeySetting(ShadowsocksController controller) {
            this.controller = controller;
            InitializeComponent();
            Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
        }
        
        private void HotkeySetting_Load(object sender, EventArgs e) {
            I18n();

            //Load Config
            HotkeyConfig conf = controller.GetConfigurationCopy().hotkey;
            if(conf == null) {
                conf = new HotkeyConfig();
            }
            textBox1.Text = conf.SwitchSystemProxy;
            textBox2.Text = conf.ChangeToPac;
            textBox3.Text = conf.ChangeToGlobal;
            textBox4.Text = conf.SwitchAllowLan;
            textBox5.Text = conf.ShowLogs;
            checkBox1.Checked = conf.AllowSwitchServer;
        }
        
        private void I18n() {
            Text = I18N.GetString(Text);
            label1.Text = I18N.GetString(label1.Text);
            label2.Text = I18N.GetString(label2.Text);
            label3.Text = I18N.GetString(label3.Text);
            label4.Text = I18N.GetString(label4.Text);
            label5.Text = I18N.GetString(label5.Text);
            checkBox1.Text = I18N.GetString(checkBox1.Text);
            ok.Text = I18N.GetString(ok.Text);
            cancel.Text = I18N.GetString(cancel.Text);
        }

        private StringBuilder sb = new StringBuilder();

        /// <summary>
        /// Capture hotkey - Press key
        /// </summary>
        private void HotkeyDown(object sender, KeyEventArgs e) {
            sb.Length = 0;
            //Combination key only
            if(e.Modifiers != 0) {
                if(e.Control) {
                    sb.Append("Ctrl + ");
                }
                if (e.Shift)
                {
                    sb.Append("Shift + ");
                }
                if (e.Alt) {
                    sb.Append("Alt + ");
                }
                
                Keys keyvalue = (Keys)e.KeyValue;
                if((keyvalue >= Keys.PageUp && keyvalue <= Keys.Down) ||
                    (keyvalue >= Keys.A && keyvalue <= Keys.Z) ||
                    (keyvalue >= Keys.F1 && keyvalue <= Keys.F12)) {
                    sb.Append(e.KeyCode);
                } else if(keyvalue >= Keys.D0 && keyvalue <= Keys.D9) {
                    sb.Append('D').Append((char)e.KeyValue);
                } else if(keyvalue >= Keys.NumPad0 && keyvalue <= Keys.NumPad9) {
                    sb.Append("NumPad").Append((char)(e.KeyValue - 48));
                }
            }
            ((TextBox)sender).Text = sb.ToString();
        }

        /// <summary>
        /// Capture hotkey - Release key
        /// </summary>
        private void HotkeyUp(object sender, KeyEventArgs e) {
            TextBox tb = sender as TextBox;
            string content = tb.Text.TrimEnd();
            if(content.Length >= 1 && content[content.Length - 1] == '+') {
                tb.Text = "";
            }
        }
        
        private void cancel_Click(object sender, EventArgs e) {
            this.Close();
        }
        

        private void ok_Click(object sender, EventArgs e) {
            //Save Config
            HotkeyConfig conf = controller.GetConfigurationCopy().hotkey;

            var allTextboxes = HotKeys.GetChildControls<TextBox>( this );
            foreach ( TextBox tb in allTextboxes ) {
                if ( HotKeys.ParseHotKeyFromScreen( tb.Text.ToString() ) == null ) {
                    // parse err
                    MessageBox.Show( "Can not parse" );
                    return;
                }

            }
            // try to register keys

            // write into config
            WriteHotKeyConfig( conf );

            this.Close();
        }

        private void WriteHotKeyConfig( HotkeyConfig conf ) {
            conf.SwitchSystemProxy = textBox1.Text;
            conf.ChangeToPac = textBox2.Text;
            conf.ChangeToGlobal = textBox3.Text;
            conf.SwitchAllowLan = textBox4.Text;
            conf.ShowLogs = textBox5.Text;
            conf.AllowSwitchServer = checkBox1.Checked;
            controller.SaveHotkeyConfig( conf );
        }
    }
}
