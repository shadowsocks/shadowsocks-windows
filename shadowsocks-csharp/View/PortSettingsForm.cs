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

            LoadCurrentConfiguration();
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
            foreach(Server s in configuration.configs)
            {
                comboServers.Items.Add(GetDisplayText(s));
            }
            listPorts.Items.Clear();
            int[] list = new int[configuration.portMap.Count];
            int list_index = 0;
            foreach (KeyValuePair<string, object> it in configuration.portMap)
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
                if (key != textLocal.Text)
                {
                    if (_modifiedConfiguration.portMap.ContainsKey(key))
                    {
                        _modifiedConfiguration.portMap.Remove(key);
                    }
                    reflash_list = true;
                    key = textLocal.Text;
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
                cfg.type = comboBoxType.SelectedIndex;
                cfg.id = GetID(comboServers.Text);
                cfg.server_addr = textAddr.Text;
                if (cfg.remarks != textRemarks.Text)
                {
                    reflash_list = true;
                }
                cfg.remarks = textRemarks.Text;
                try
                {
                    cfg.server_port = int.Parse(textPort.Text);
                }
                catch(FormatException)
                {
                    cfg.server_port = 0;
                }
                if (reflash_list)
                {
                    LoadConfiguration(_modifiedConfiguration);
                }
            }
        }

        private void LoadSelectedServer()
        {
            string key = ServerListText2Key((string)listPorts.SelectedItem);
            if (key != null && _modifiedConfiguration.portMap.ContainsKey(key))
            {
                PortMapConfig cfg = _modifiedConfiguration.portMap[key] as PortMapConfig;

                checkEnable.Checked = cfg.enable;
                comboBoxType.SelectedIndex = cfg.type;
                comboServers.Text = GetIDText(cfg.id);
                textLocal.Text = key;
                textAddr.Text = cfg.server_addr;
                textPort.Text = cfg.server_port.ToString();
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
            cfg.type = comboBoxType.SelectedIndex;
            cfg.id = GetID(comboServers.Text);
            cfg.server_addr = textAddr.Text;
            cfg.remarks = textRemarks.Text;
            try
            {
                cfg.server_port = int.Parse(textPort.Text);
            }
            catch (FormatException)
            {
                cfg.server_port = 0;
            }

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
                textPort.ReadOnly = false;
            }
            else
            {
                textAddr.ReadOnly = true;
                textPort.ReadOnly = true;
            }
        }
    }
}
