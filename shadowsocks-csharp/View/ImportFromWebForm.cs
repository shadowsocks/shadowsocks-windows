using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using Shadowsocks.Controller;
using System.Windows.Forms;
using System.Collections;
using Shadowsocks.Model;

namespace Shadowsocks.View
{
    public partial class ImportFromWebForm : Form
    {
        private int _lastSelectedIndex;
        public List<Server> Configs = new List<Server>();
        public ImportFromWebForm()
        {
            this.Font = System.Drawing.SystemFonts.MessageBoxFont;
            InitializeComponent();
            UpdateTexts();

        }
        private void UpdateTexts()
        {
            this.Text = I18N.GetString("Import");
            AddConfigButton.Text = I18N.GetString("&Add Config");
            ProviderURLLabel.Text = I18N.GetString("URL");
            GetConfigButton.Text = I18N.GetString("&Get");
            RemoveButton.Text = I18N.GetString("&Remove");

        }

        private void ImportFromWebForm_Load(object sender, EventArgs e)
        {

        }

        private void GetConfigButton_Click(object sender, EventArgs e)
        {
            
            OnlineConfigController Config = new OnlineConfigController(URLTextBox.Text);
            try
            {
                Server[] RawServers = Config.GetOnlineConfig();
                this.Configs.Clear();
                ConfigListBox.Items.Clear();
                foreach (Server TempServer in RawServers)
                {
                    Configs.Add(TempServer);
                    ConfigListBox.Items.Add(TempServer.FriendlyName());
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
            
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            _lastSelectedIndex = ConfigListBox.SelectedIndex;
            if(_lastSelectedIndex>=0 && _lastSelectedIndex < Configs.Count)
            {
                Configs.RemoveAt(_lastSelectedIndex);
            }
            if (_lastSelectedIndex >= Configs.Count)
            {
                // can be -1
                _lastSelectedIndex = Configs.Count - 1;
            }
            ConfigListBox.Items.Clear();
            foreach (Server TempServer in Configs)
            {
                ConfigListBox.Items.Add(TempServer.FriendlyName());
            }
        }

        private void AddConfigButton_Click(object sender, EventArgs e)
        {
            if (this.Configs.Count < 1)
            {
                string Message = "No valid configs to import!";
                Message = I18N.GetString("No valid configs to import!");
                MessageBox.Show(Message);
            }
            else
            {
                ConfigForm config = (ConfigForm)this.Owner;
                config._onlineConfig = this.Configs;
                this.Close();
            }
            
        }
    }
}
