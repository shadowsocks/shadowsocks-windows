using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;

namespace Shadowsocks.View
{
    public partial class PortSettingsForm : Form
    {
        private ShadowsocksController controller;
        private Configuration _modifiedConfiguration;
        private int _oldSelectedIndex = -1;

        public PortSettingsForm(ShadowsocksController controller)
        {
            InitializeComponent();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
            this.controller = controller;
            controller.ConfigChanged += controller_ConfigChanged;

            UpdateTexts();

            comboBoxType.DisplayMember = "Text";
            comboBoxType.ValueMember = "Value";
            var items = new[]
            {
                new {Text = I18N.GetString("Port Forward"), Value = PortMapType.Forward},
                new {Text = I18N.GetString("Force Proxy"), Value = PortMapType.ForceProxy},
                new {Text = I18N.GetString("Proxy With Rule"), Value = PortMapType.RuleProxy}
            };
            comboBoxType.DataSource = items;

            LoadCurrentConfiguration();
        }

        private void UpdateTexts()
        {
            this.Text = I18N.GetString("Port Settings");
            groupBox1.Text = I18N.GetString("Map Setting");
            labelType.Text = I18N.GetString("Type");
            labelID.Text = I18N.GetString("Server ID");
            labelAddr.Text = I18N.GetString("Target Addr");
            labelPort.Text = I18N.GetString("Target Port");
            checkEnable.Text = I18N.GetString("Enable");
            labelLocal.Text = I18N.GetString("Local Port");
            label1.Text = I18N.GetString("Remarks");
            OKButton.Text = I18N.GetString("OK");
            MyCancelButton.Text = I18N.GetString("Cancel");
            Add.Text = I18N.GetString("&Add");
            Del.Text = I18N.GetString("&Delete");
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void PortMapForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            controller.ConfigChanged -= controller_ConfigChanged;
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = controller.GetConfiguration();
            LoadConfiguration(_modifiedConfiguration);
            LoadSelectedServer();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            SaveSelectedServer();
            controller.SaveServersPortMap(_modifiedConfiguration);
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void LoadConfiguration(Configuration configuration)
        {
            comboServers.Items.Clear();
            comboServers.Items.Add("");
            Dictionary<string, int> server_group = new Dictionary<string, int>();
            foreach (Server s in configuration.configs)
            {
                if (s.group != null && s.group.Length > 0 && !server_group.ContainsKey(s.group))
                {
                    comboServers.Items.Add("#" + s.group);
                    server_group[s.group] = 1;
                }
            }
            foreach (Server s in configuration.configs)
            {
                comboServers.Items.Add(GetDisplayText(s));
            }
            listPorts.Items.Clear();
            int[] list = new int[configuration.portMap.Count];
            int list_index = 0;
            foreach (KeyValuePair<string, PortMapConfig> it in configuration.portMap)
            {
                try
                {
                    list[list_index] = int.Parse(it.Key);
                }
                catch (FormatException)
                {

                }
                list_index += 1;
            }
            Array.Sort(list);
            for (int i = 0; i < list.Length; ++i)
            {
                string remarks = "";
                remarks = ((PortMapConfig)configuration.portMap[list[i].ToString()]).remarks ?? "";
                listPorts.Items.Add(list[i].ToString() + "    " + remarks);
            }
            _oldSelectedIndex = -1;
            if (listPorts.Items.Count > 0)
            {
                listPorts.SelectedIndex = 0;
            }
        }

        private string ServerListText2Key(string text)
        {
            if (text != null)
            {
                int pos = text.IndexOf(' ');
                if (pos > 0)
                    return text.Substring(0, pos);
            }
            return text;
        }

        private void SaveSelectedServer()
        {
            if (_oldSelectedIndex != -1)
            {
                bool reflash_list = false;
                string key = _oldSelectedIndex.ToString();
                if (key != NumLocalPort.Text)
                {
                    if (_modifiedConfiguration.portMap.ContainsKey(key))
                    {
                        _modifiedConfiguration.portMap.Remove(key);
                    }
                    reflash_list = true;
                    key = NumLocalPort.Text;
                    try
                    {
                        _oldSelectedIndex = int.Parse(key);
                    }
                    catch (FormatException)
                    {
                        _oldSelectedIndex = 0;
                    }
                }
                if (!_modifiedConfiguration.portMap.ContainsKey(key))
                {
                    _modifiedConfiguration.portMap[key] = new PortMapConfig();
                }
                PortMapConfig cfg = _modifiedConfiguration.portMap[key] as PortMapConfig;

                cfg.enable = checkEnable.Checked;
                cfg.type = (PortMapType) comboBoxType.SelectedValue;
                cfg.id = GetID(comboServers.Text);
                cfg.server_addr = textAddr.Text;
                if (cfg.remarks != textRemarks.Text)
                {
                    reflash_list = true;
                }
                cfg.remarks = textRemarks.Text;
                cfg.server_port = Convert.ToInt32(NumTargetPort.Value);
                if (reflash_list)
                {
                    LoadConfiguration(_modifiedConfiguration);
                }
            }
        }

        private void LoadSelectedServer()
        {
            string key = ServerListText2Key((string)listPorts.SelectedItem);
            Dictionary<string, int> server_group = new Dictionary<string, int>();
            foreach (Server s in _modifiedConfiguration.configs)
            {
                if (s.group != null && s.group.Length > 0 && !server_group.ContainsKey(s.group))
                {
                    server_group[s.group] = 1;
                }
            }
            if (key != null && _modifiedConfiguration.portMap.ContainsKey(key))
            {
                PortMapConfig cfg = _modifiedConfiguration.portMap[key] as PortMapConfig;

                checkEnable.Checked = cfg.enable;
                comboBoxType.SelectedValue = cfg.type;
                string text = GetIDText(cfg.id);
                if (text.Length == 0 && server_group.ContainsKey(cfg.id))
                {
                    text = "#" + cfg.id;
                }
                comboServers.Text = text;
                NumLocalPort.Text = key;
                textAddr.Text = cfg.server_addr;
                NumTargetPort.Value = cfg.server_port;
                textRemarks.Text = cfg.remarks ?? "";

                try
                {
                    _oldSelectedIndex = int.Parse(key);
                }
                catch (FormatException)
                {
                    _oldSelectedIndex = 0;
                }
            }
        }

        private string GetID(string text)
        {
            if (text.IndexOf('#') >= 0)
            {
                return text.Substring(text.IndexOf('#') + 1);
            }
            return text;
        }

        private string GetDisplayText(Server s)
        {
            return (s.group != null && s.group.Length > 0 ? s.group + " - " : "    - ") + s.FriendlyName() + "        #" + s.id;
        }

        private string GetIDText(string id)
        {
            foreach (Server s in _modifiedConfiguration.configs)
            {
                if (id == s.id)
                {
                    return GetDisplayText(s);
                }
            }
            return "";
        }

        private void listPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            SaveSelectedServer();
            LoadSelectedServer();
        }

        private void Add_Click(object sender, EventArgs e)
        {
            string key = "0";
            if (!_modifiedConfiguration.portMap.ContainsKey(key))
            {
                _modifiedConfiguration.portMap[key] = new PortMapConfig();
            }
            PortMapConfig cfg = _modifiedConfiguration.portMap[key] as PortMapConfig;

            cfg.enable = checkEnable.Checked;
            cfg.type = (PortMapType) comboBoxType.SelectedValue;
            cfg.id = GetID(comboServers.Text);
            cfg.server_addr = textAddr.Text;
            cfg.remarks = textRemarks.Text;
            cfg.server_port = Convert.ToInt32(NumTargetPort.Value);

            _oldSelectedIndex = -1;
            LoadConfiguration(_modifiedConfiguration);
            LoadSelectedServer();
        }

        private void Del_Click(object sender, EventArgs e)
        {
            string key = _oldSelectedIndex.ToString();
            if (_modifiedConfiguration.portMap.ContainsKey(key))
            {
                _modifiedConfiguration.portMap.Remove(key);
            }
            _oldSelectedIndex = -1;
            LoadConfiguration(_modifiedConfiguration);
            LoadSelectedServer();
        }

        private void comboBoxType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxType.SelectedIndex == 0)
            {
                textAddr.ReadOnly = false;
                NumTargetPort.ReadOnly = false;
                NumTargetPort.Increment = 1;
            }
            else
            {
                textAddr.ReadOnly = true;
                NumTargetPort.ReadOnly = true;
                NumTargetPort.Increment = 0;
            }
        }
    }
}
