using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shadowsocks.Controller;
using Shadowsocks.Model;

namespace Shadowsocks.View
{
    public partial class OnlineConfigForm : Form
    {
        private ShadowsocksController controller;
        private Configuration config;

        private const string DefaultPrefix = "http://www.baidu.com/";

        public OnlineConfigForm(ShadowsocksController controller)
        {
            this.controller = controller;
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            config = controller.GetConfigurationCopy();
            var idx = UrlListBox.SelectedIndex;
            UrlListBox.Items.Clear();

            foreach (var item in config.onlineConfigSource)
            {
                UrlListBox.Items.Add(item);
            }

            if (idx >= UrlListBox.Items.Count) idx = 0;
            UrlListBox.SelectedIndex = idx;
            SelectItem();
        }

        private void SelectItem()
        {
            UrlTextBox.Text = (string)UrlListBox.SelectedItem;
        }

        private bool ValidateUrl()
        {
            try
            {
                var scheme = new Uri(UrlTextBox.Text).Scheme;
                if (scheme != "http" && scheme != "https") return false;
            }
            catch
            {
                return false;
            }
            return true;
        }

        private bool Commit()
        {
            if (ValidateUrl())
            {
                UrlListBox.Items[UrlListBox.SelectedIndex] = UrlTextBox.Text;
            }
            controller.SaveOnlineConfigSource(UrlListBox.Items.OfType<string>().Where(s => !string.IsNullOrWhiteSpace(s)));
            LoadConfig();
            return true;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            Commit();
            UrlListBox.Items.Add("");
            UrlTextBox.Text = DefaultPrefix;
            UrlListBox.SelectedIndex = UrlListBox.Items.Count - 1;
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            // update content, also update online config
            Commit();

            _ = controller.UpdateOnlineConfig(UrlTextBox.Text);
        }

        private void UrlListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!UrlListBox.CanSelect)
            {
                return;
            }
            SelectItem();
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            int idx = UrlListBox.SelectedIndex;
            UrlListBox.Items.RemoveAt(idx);
            Commit();
        }

        private void UpdateAllButton_Click(object sender, EventArgs e)
        {
            _ = controller.UpdateAllOnlineConfig();
        }
    }
}
