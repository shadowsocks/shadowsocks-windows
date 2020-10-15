using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;

namespace Shadowsocks.View
{
    public partial class OnlineConfigForm : Form
    {
        private ShadowsocksController controller;
        private Configuration config;

        public OnlineConfigForm(ShadowsocksController controller)
        {
            this.controller = controller;
            InitializeComponent();
            LoadConfig();
            Icon = System.Drawing.Icon.FromHandle(Resources.ssw128.GetHicon());
            I18N.TranslateForm(this);
        }

        private void LoadConfig()
        {
            config = controller.GetCurrentConfiguration();
            var idx = UrlListBox.SelectedIndex;
            UrlListBox.Items.Clear();

            foreach (var item in config.onlineConfigSource)
            {
                UrlListBox.Items.Add(item);
            }

            if (idx >= UrlListBox.Items.Count) idx = 0;
            if (idx < 0 && UrlListBox.Items.Count > 0) idx = 0;
            if (UrlListBox.Items.Count == 0) return;
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
                if (UrlListBox.Items.OfType<string>().Contains(UrlTextBox.Text)) return false;
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void Commit()
        {
            if (UrlListBox.SelectedIndex < 0) return;
            if ((string)UrlListBox.SelectedItem == UrlTextBox.Text)
            {
                LoadConfig();
                return;
            }

            if (ValidateUrl())
            {

                UrlListBox.Items[UrlListBox.SelectedIndex] = UrlTextBox.Text;
            }
            controller.SaveOnlineConfigSource(UrlListBox.Items.OfType<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct());
            LoadConfig();
            return;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UrlTextBox.Text)) return;
            UrlListBox.Items.Add(UrlTextBox.Text);
            UrlListBox.SelectedIndex = UrlListBox.Items.Count - 1;
            UrlTextBox.Text = "";
            Commit();
        }

        private async void UpdateButton_Click(object sender, EventArgs e)
        {
            string old = (string)UrlListBox.SelectedItem;
            // update content, also update online config
            Commit();
            string current = (string)UrlListBox.SelectedItem;
            if (UrlListBox.Items.Count == 0) return;
            tableLayoutPanel1.Enabled = false;
            bool ok = await controller.UpdateOnlineConfig(current);
            if (!ok)
            {
                MessageBox.Show(I18N.GetString("online config failed to update"));
                tableLayoutPanel1.Enabled = true;
                return;
            }
            if (old != current) controller.RemoveOnlineConfig(old);
            tableLayoutPanel1.Enabled = true;
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
            if (UrlListBox.Items.Count == 0) return;
            string url = (string)UrlListBox.SelectedItem;
            if (!string.IsNullOrWhiteSpace(url))
            {
                controller.RemoveOnlineConfig(url);
            }
            LoadConfig();
        }

        private async void UpdateAllButton_Click(object sender, EventArgs e)
        {
            if (UrlListBox.Items.Count == 0) return;
            tableLayoutPanel1.Enabled = false;
            int fail = await controller.UpdateAllOnlineConfig();
            if (fail > 0)
            {
                MessageBox.Show(I18N.GetString("{0} online config failed to update", fail));
            }
            tableLayoutPanel1.Enabled = true;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            Commit();
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
