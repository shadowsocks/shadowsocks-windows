using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using ZXing.QrCode.Internal;
using Shadowsocks.Encryption;

namespace Shadowsocks.View
{
    public partial class ConfigForm : Form
    {
        private ShadowsocksController controller;
        private UpdateChecker updateChecker;

        // this is a copy of configuration that we are working on
        private Configuration _modifiedConfiguration;
        private int _oldSelectedIndex = -1;
        private string _oldSelectedID = null;

        private string _SelectedID = null;

        public ConfigForm(ShadowsocksController controller, UpdateChecker updateChecker, int focusIndex)
        {
            this.Font = System.Drawing.SystemFonts.MessageBoxFont;
            InitializeComponent();
            try
            {
                ServersListBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            }
            catch
            {
                ServersListBox.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            }


            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
            this.controller = controller;
            this.updateChecker = updateChecker;
            if (updateChecker.LatestVersionURL == null)
                LinkUpdate.Visible = false;

            foreach (string name in EncryptorFactory.GetEncryptor())
            {
                EncryptorInfo info = EncryptorFactory.GetEncryptorInfo(name);
                if (info.display)
                    EncryptionSelect.Items.Add(name);
            }
            UpdateTexts();
            controller.ConfigChanged += controller_ConfigChanged;

            LoadCurrentConfiguration();
            if (_modifiedConfiguration.index >= 0 && _modifiedConfiguration.index < _modifiedConfiguration.configs.Count)
                _oldSelectedID = _modifiedConfiguration.configs[_modifiedConfiguration.index].id;
            if (focusIndex == -1)
            {
                int index = _modifiedConfiguration.index + 1;
                if (index < 0 || index > _modifiedConfiguration.configs.Count)
                    index = _modifiedConfiguration.configs.Count;

                focusIndex = index;
            }
            if (focusIndex >= 0 && focusIndex < _modifiedConfiguration.configs.Count)
                SetServerListSelectedIndex(focusIndex);

            if (_modifiedConfiguration.isHideTips)
                PictureQRcode.Visible = false;

            int dpi_mul = Util.Utils.GetDpiMul();
            ServersListBox.Width = ServersListBox.Width * dpi_mul / 4;
            ServersListBox.Height = ServersListBox.Height * dpi_mul / 4;
            AddButton.Width = AddButton.Width * dpi_mul / 4;
            AddButton.Height = AddButton.Height * dpi_mul / 4;
            DeleteButton.Width = DeleteButton.Width * dpi_mul / 4;
            DeleteButton.Height = DeleteButton.Height * dpi_mul / 4;
            UpButton.Width = UpButton.Width * dpi_mul / 4;
            UpButton.Height = UpButton.Height * dpi_mul / 4;
            DownButton.Width = DownButton.Width * dpi_mul / 4;
            DownButton.Height = DownButton.Height * dpi_mul / 4;

            //IPTextBox.Width = IPTextBox.Width * dpi_mul / 4;
            //ServerPortTextBox.Width = ServerPortTextBox.Width * dpi_mul / 4;
            //PasswordTextBox.Width = PasswordTextBox.Width * dpi_mul / 4;
            //EncryptionSelect.Width = EncryptionSelect.Width * dpi_mul / 4;
            //TCPProtocolComboBox.Width = TCPProtocolComboBox.Width * dpi_mul / 4;
            //ObfsCombo.Width = ObfsCombo.Width * dpi_mul / 4;
            //TextObfsParam.Width = TextObfsParam.Width * dpi_mul / 4;
            //RemarksTextBox.Width = RemarksTextBox.Width * dpi_mul / 4;
            //TextGroup.Width = TextGroup.Width * dpi_mul / 4;
            //TextLink.Width = TextLink.Width * dpi_mul / 4;
            //TextUDPPort.Width = TextUDPPort.Width * dpi_mul / 4;
            //Font new_font = new Font("Arial", (float)(9.0 * dpi_mul / 4));
            //this.Font = new_font;
            //IPTextBox.Font = new_font;
            //ServerPortTextBox.Font = new_font;
            //PasswordTextBox.Font = new_font;
            //EncryptionSelect.Font = new_font;
            //TCPProtocolComboBox.Font = new_font;
            //ObfsCombo.Font = new_font;
            //TextObfsParam.Font = new_font;
            //RemarksTextBox.Font = new_font;
            //TextGroup.Font = new_font;
            //TextLink.Font = new_font;
            //TextUDPPort.Font = new_font;

            //int font_height = 9;
            //EncryptionSelect.Height = EncryptionSelect.Height - font_height + font_height * dpi_mul / 4;
            //TCPProtocolComboBox.Height = TCPProtocolComboBox.Height - font_height + font_height * dpi_mul / 4;
            //ObfsCombo.Height = ObfsCombo.Height - font_height + font_height * dpi_mul / 4;

            ////MyCancelButton.Height = MyCancelButton.Height * dpi_mul / 4;
            //MyCancelButton.Width = MyCancelButton.Width * dpi_mul / 4;
            //OKButton.Width = OKButton.Width * dpi_mul / 4;
            ////OKButton.Height = OKButton.Height * dpi_mul / 4;

            DrawLogo(350 * dpi_mul / 4);
        }

        private void UpdateTexts()
        {
            this.Text = I18N.GetString("Edit Servers") + "("
                + (controller.GetCurrentConfiguration().shareOverLan ? "any" : "local") + ":" + controller.GetCurrentConfiguration().localPort.ToString()
                + I18N.GetString(" Version") + UpdateChecker.FullVersion
                + ")";

            AddButton.Text = I18N.GetString("&Add");
            DeleteButton.Text = I18N.GetString("&Delete");
            UpButton.Text = I18N.GetString("Up");
            DownButton.Text = I18N.GetString("Down");

            const string mark_str = "* ";
            IPLabel.Text = mark_str + I18N.GetString("Server IP");
            ServerPortLabel.Text = mark_str + I18N.GetString("Server Port");
            labelUDPPort.Text = I18N.GetString("UDP Port");
            PasswordLabel.Text = mark_str + I18N.GetString("Password");
            EncryptionLabel.Text = mark_str + I18N.GetString("Encryption");
            TCPProtocolLabel.Text = mark_str + I18N.GetString(TCPProtocolLabel.Text);
            labelObfs.Text = mark_str + I18N.GetString(labelObfs.Text);
            labelRemarks.Text = I18N.GetString("Remarks");

            checkAdvSetting.Text = I18N.GetString(checkAdvSetting.Text);
            TCPoverUDPLabel.Text = I18N.GetString(TCPoverUDPLabel.Text);
            UDPoverTCPLabel.Text = I18N.GetString(UDPoverTCPLabel.Text);
            labelObfsParam.Text = I18N.GetString(labelObfsParam.Text);
            ObfsUDPLabel.Text = I18N.GetString(ObfsUDPLabel.Text);
            LabelNote.Text = I18N.GetString(LabelNote.Text);
            CheckTCPoverUDP.Text = I18N.GetString(CheckTCPoverUDP.Text);
            CheckUDPoverUDP.Text = I18N.GetString(CheckUDPoverUDP.Text);
            CheckObfsUDP.Text = I18N.GetString(CheckObfsUDP.Text);
            checkSSRLink.Text = I18N.GetString(checkSSRLink.Text);
            for (int i = 0; i < TCPProtocolComboBox.Items.Count; ++i)
            {
                TCPProtocolComboBox.Items[i] = I18N.GetString(TCPProtocolComboBox.Items[i].ToString());
            }

            ServerGroupBox.Text = I18N.GetString("Server");

            OKButton.Text = I18N.GetString("OK");
            MyCancelButton.Text = I18N.GetString("Cancel");
            LinkUpdate.MaximumSize = new Size(ServersListBox.Width, ServersListBox.Height);
            LinkUpdate.Text = String.Format(I18N.GetString("New version {0} {1} available"), UpdateChecker.Name, updateChecker.LatestVersionNumber);
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void ShowWindow()
        {
            this.Opacity = 1;
            this.Show();
            IPTextBox.Focus();
        }

        private int SaveOldSelectedServer()
        {
            try
            {
                if (_oldSelectedIndex == -1 || _oldSelectedIndex >= _modifiedConfiguration.configs.Count)
                {
                    return 0; // no changes
                }
                Server server = new Server
                {
                    server = IPTextBox.Text.Trim(),
                    server_port = int.Parse(ServerPortTextBox.Text),
                    server_udp_port = int.Parse(TextUDPPort.Text),
                    password = PasswordTextBox.Text,
                    method = EncryptionSelect.Text,
                    obfs = ObfsCombo.Text,
                    obfsparam = TextObfsParam.Text,
                    remarks = RemarksTextBox.Text,
                    group = TextGroup.Text.Trim(),
                    udp_over_tcp = CheckUDPoverUDP.Checked,
                    protocol = TCPProtocolComboBox.Text,
                    //obfs_udp = CheckObfsUDP.Checked,
                    id = _SelectedID
                };
                Configuration.CheckServer(server);
                int ret = 0;
                if (_modifiedConfiguration.configs[_oldSelectedIndex].server != server.server
                    || _modifiedConfiguration.configs[_oldSelectedIndex].server_port != server.server_port
                    || _modifiedConfiguration.configs[_oldSelectedIndex].remarks_base64 != server.remarks_base64
                    || _modifiedConfiguration.configs[_oldSelectedIndex].group != server.group
                    )
                {
                    ret = 1; // display changed
                }
                Server oldServer = _modifiedConfiguration.configs[_oldSelectedIndex];
                if (oldServer.server == server.server
                    && oldServer.server_port == server.server_port
                    && oldServer.password == server.password
                    && oldServer.method == server.method
                    && oldServer.obfs == server.obfs
                    && oldServer.obfsparam == server.obfsparam
                    )
                {
                    server.setObfsData(oldServer.getObfsData());
                }
                _modifiedConfiguration.configs[_oldSelectedIndex] = server;

                return ret;
            }
            catch (FormatException)
            {
                MessageBox.Show(I18N.GetString("Illegal port number format"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return -1; // ERROR
        }

        private void DrawLogo(int width)
        {
            Bitmap drawArea = new Bitmap(width, width);
            using (Graphics g = Graphics.FromImage(drawArea))
            {
                g.Clear(Color.White);
                Bitmap ngnl = Resources.ngnl;
                g.DrawImage(ngnl, new Rectangle(0, 0, width, width));
                if (!_modifiedConfiguration.isHideTips)
                    g.DrawString("Click the 'Link' text box", new Font("Arial", 14), new SolidBrush(Color.Black), new RectangleF(0, 0, 300, 300));
            }
            PictureQRcode.Image = drawArea;
        }

        private void GenQR(string ssconfig)
        {
            int dpi_mul = Util.Utils.GetDpiMul();
            int width = 350 * dpi_mul / 4;
            if (TextLink.Focused)
            {
                string qrText = ssconfig;
                QRCode code = ZXing.QrCode.Internal.Encoder.encode(qrText, ErrorCorrectionLevel.M);
                ByteMatrix m = code.Matrix;
                int blockSize = Math.Max(width / (m.Width + 2), 1);
                Bitmap drawArea = new Bitmap(((m.Width + 2) * blockSize), ((m.Height + 2) * blockSize));
                using (Graphics g = Graphics.FromImage(drawArea))
                {
                    g.Clear(Color.White);
                    using (Brush b = new SolidBrush(Color.Black))
                    {
                        for (int row = 0; row < m.Width; row++)
                        {
                            for (int col = 0; col < m.Height; col++)
                            {
                                if (m[row, col] != 0)
                                {
                                    g.FillRectangle(b, blockSize * (row + 1), blockSize * (col + 1),
                                        blockSize, blockSize);
                                }
                            }
                        }
                    }
                    Bitmap ngnl = Resources.ngnl;
                    int div = 13, div_l = 5, div_r = 8;
                    int l = (m.Width * div_l + div - 1) / div * blockSize, r = (m.Width * div_r + div - 1) / div * blockSize;
                    g.DrawImage(ngnl, new Rectangle(l + blockSize, l + blockSize, r - l, r - l));
                }
                PictureQRcode.Image = drawArea;
                PictureQRcode.Visible = true;
                _modifiedConfiguration.isHideTips = true;
            }
            else
            {
                //PictureQRcode.Visible = false;
                DrawLogo(PictureQRcode.Width);
            }
        }

        private void LoadSelectedServer()
        {
            if (ServersListBox.SelectedIndex >= 0 && ServersListBox.SelectedIndex < _modifiedConfiguration.configs.Count)
            {
                Server server = _modifiedConfiguration.configs[ServersListBox.SelectedIndex];

                IPTextBox.Text = server.server;
                ServerPortTextBox.Text = server.server_port.ToString();
                TextUDPPort.Text = server.server_udp_port.ToString();
                PasswordTextBox.Text = server.password;
                EncryptionSelect.Text = server.method ?? "aes-256-cfb";
                if (server.protocol == null || server.protocol.Length == 0)
                {
                    TCPProtocolComboBox.Text = "origin";
                }
                else
                {
                    TCPProtocolComboBox.Text = server.protocol ?? "origin";
                }
                string obfs_text = server.obfs ?? "plain";
                TextObfsParam.Text = server.obfsparam;
                RemarksTextBox.Text = server.remarks;
                TextGroup.Text = server.group;
                CheckUDPoverUDP.Checked = server.udp_over_tcp;
                //CheckObfsUDP.Checked = server.obfs_udp;
                _SelectedID = server.id;

                ServerGroupBox.Visible = true;

                if (TCPProtocolComboBox.Text == "origin"
                    && obfs_text == "plain"
                    && !CheckUDPoverUDP.Checked
                    )
                {
                    CheckSSR.Checked = false;
                    CheckSSR.Enabled = true;
                    checkAdvSetting.Checked = false;
                }
                else
                {
                    CheckSSR.Checked = true;
                    CheckSSR.Enabled = false;
                }

                if (checkSSRLink.Checked && CheckSSR.Checked)
                {
                    TextLink.Text = controller.GetSSRRemarksLinkForServer(server);
                }
                else
                {
                    TextLink.Text = controller.GetSSLinkForServer(server);
                }

                if (CheckTCPoverUDP.Checked || CheckUDPoverUDP.Checked || server.server_udp_port != 0)
                {
                    checkAdvSetting.Checked = true;
                }

                PasswordLabel.Checked = false;
                Update_SSR_controls_Visable();
                ObfsCombo.Text = obfs_text;
                GenQR(TextLink.Text);
                //IPTextBox.Focus();
            }
            else
            {
                ServerGroupBox.Visible = false;
            }
        }

        private void LoadConfiguration(Configuration configuration)
        {
            if (ServersListBox.Items.Count != _modifiedConfiguration.configs.Count)
            {
                ServersListBox.Items.Clear();
                foreach (Server server in _modifiedConfiguration.configs)
                {
                    if (server.group != null && server.group.Length > 0)
                    {
                        ServersListBox.Items.Add(server.group + " - " + server.FriendlyName());
                    }
                    else
                    {
                        ServersListBox.Items.Add("      " + server.FriendlyName());
                    }
                }
            }
            else
            {
                for (int i = 0; i < _modifiedConfiguration.configs.Count; ++i)
                {
                    if (_modifiedConfiguration.configs[i].group != null && _modifiedConfiguration.configs[i].group.Length > 0)
                    {
                        ServersListBox.Items[i] = _modifiedConfiguration.configs[i].group + " - " + _modifiedConfiguration.configs[i].FriendlyName();
                    }
                    else
                    {
                        ServersListBox.Items[i] = "      " + _modifiedConfiguration.configs[i].FriendlyName();
                    }
                }
            }
        }

        private void SetServerListSelectedIndex(int index)
        {
            int oldSelectedIndex = _oldSelectedIndex;
            int selIndex = Math.Min(index + 5, ServersListBox.Items.Count - 1);
            if (selIndex != index)
            {
                _oldSelectedIndex = selIndex;
                ServersListBox.SelectedIndex = selIndex;
                _oldSelectedIndex = oldSelectedIndex;
                ServersListBox.SelectedIndex = index;
            }
            else
            {
                ServersListBox.SelectedIndex = index;
            }
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = controller.GetConfiguration();
            LoadConfiguration(_modifiedConfiguration);
            SetServerListSelectedIndex(_modifiedConfiguration.index);
            LoadSelectedServer();
        }

        private void ServersListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_oldSelectedIndex == ServersListBox.SelectedIndex || ServersListBox.SelectedIndex == -1)
            {
                // we are moving back to oldSelectedIndex or doing a force move
                return;
            }
            int change = SaveOldSelectedServer();
            if (change == -1)
            {
                ServersListBox.SelectedIndex = _oldSelectedIndex; // go back
                return;
            }
            if (change == 1)
            {
                LoadConfiguration(_modifiedConfiguration);
            }
            LoadSelectedServer();
            _oldSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (SaveOldSelectedServer() == -1)
            {
                return;
            }
            Server server = _oldSelectedIndex >=0 && _oldSelectedIndex < _modifiedConfiguration.configs.Count
                ? Configuration.CopyServer(_modifiedConfiguration.configs[_oldSelectedIndex])
                : Configuration.GetDefaultServer();
            _modifiedConfiguration.configs.Insert(_oldSelectedIndex, server);
            LoadConfiguration(_modifiedConfiguration);
            ServersListBox.SelectedIndex = _oldSelectedIndex + 1;
            _oldSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            _oldSelectedIndex = ServersListBox.SelectedIndex;
            if (_oldSelectedIndex >= 0 && _oldSelectedIndex < _modifiedConfiguration.configs.Count)
            {
                _modifiedConfiguration.configs.RemoveAt(_oldSelectedIndex);
            }
            if (_oldSelectedIndex >= _modifiedConfiguration.configs.Count)
            {
                // can be -1
                _oldSelectedIndex = _modifiedConfiguration.configs.Count - 1;
            }
            ServersListBox.SelectedIndex = _oldSelectedIndex;
            LoadConfiguration(_modifiedConfiguration);
            SetServerListSelectedIndex(_oldSelectedIndex);
            LoadSelectedServer();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (SaveOldSelectedServer() == -1)
            {
                return;
            }
            if (_modifiedConfiguration.configs.Count == 0)
            {
                MessageBox.Show(I18N.GetString("Please add at least one server"));
                return;
            }
            if (_oldSelectedID != null)
            {
                for (int i = 0; i < _modifiedConfiguration.configs.Count; ++i)
                {
                    if (_modifiedConfiguration.configs[i].id == _oldSelectedID)
                    {
                        _modifiedConfiguration.index = i;
                        break;
                    }
                }
            }
            controller.SaveServersConfig(_modifiedConfiguration);
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ConfigForm_Shown(object sender, EventArgs e)
        {
            IPTextBox.Focus();
        }

        private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            controller.ConfigChanged -= controller_ConfigChanged;
        }

        private void UpButton_Click(object sender, EventArgs e)
        {
            _oldSelectedIndex = ServersListBox.SelectedIndex;
            int index = _oldSelectedIndex;
            SaveOldSelectedServer();
            if (index > 0 && index < _modifiedConfiguration.configs.Count)
            {
                _modifiedConfiguration.configs.Reverse(index - 1, 2);
                _oldSelectedIndex = index - 1;
                ServersListBox.SelectedIndex = index - 1;
                LoadConfiguration(_modifiedConfiguration);
                ServersListBox.SelectedIndex = _oldSelectedIndex;
                LoadSelectedServer();
            }
        }

        private void DownButton_Click(object sender, EventArgs e)
        {
            _oldSelectedIndex = ServersListBox.SelectedIndex;
            int index = _oldSelectedIndex;
            SaveOldSelectedServer();
            if (_oldSelectedIndex >= 0 && _oldSelectedIndex < _modifiedConfiguration.configs.Count - 1)
            {
                _modifiedConfiguration.configs.Reverse(index, 2);
                _oldSelectedIndex = index + 1;
                ServersListBox.SelectedIndex = index + 1;
                LoadConfiguration(_modifiedConfiguration);
                ServersListBox.SelectedIndex = _oldSelectedIndex;
                LoadSelectedServer();
            }
        }

        private void TextBox_Enter(object sender, EventArgs e)
        {
            SaveOldSelectedServer();
            LoadSelectedServer();
            ((TextBox)sender).SelectAll();
        }

        private void TextBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ((TextBox)sender).SelectAll();
            }
        }

        private void LinkUpdate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(updateChecker.LatestVersionURL);
        }

        private void PasswordLabel_CheckedChanged(object sender, EventArgs e)
        {
            if (PasswordLabel.Checked)
            {
                PasswordTextBox.UseSystemPasswordChar = false;
            }
            else
            {
                PasswordTextBox.UseSystemPasswordChar = true;
            }
        }

        private void ObfsCombo_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Obfs.ObfsBase obfs = (Obfs.ObfsBase)Obfs.ObfsFactory.GetObfs(ObfsCombo.Text);
                int[] properties = obfs.GetObfs()[ObfsCombo.Text];
                if (properties[2] > 0)
                {
                    TextObfsParam.Enabled = true;
                }
                else
                {
                    TextObfsParam.Enabled = false;
                }
            }
            catch
            {
                TextObfsParam.Enabled = true;
            }
        }

        private void checkSSRLink_CheckedChanged(object sender, EventArgs e)
        {
            SaveOldSelectedServer();
            LoadSelectedServer();
        }

        private void checkAdvSetting_CheckedChanged(object sender, EventArgs e)
        {
            Update_SSR_controls_Visable();
        }

        private void Update_SSR_controls_Visable()
        {
            SuspendLayout();
            if (checkAdvSetting.Checked)
            {
                labelUDPPort.Visible = true;
                TextUDPPort.Visible = true;
                //TCPoverUDPLabel.Visible = true;
                //CheckTCPoverUDP.Visible = true;
            }
            else
            {
                labelUDPPort.Visible = false;
                TextUDPPort.Visible = false;
                //TCPoverUDPLabel.Visible = false;
                //CheckTCPoverUDP.Visible = false;
            }
            if (CheckSSR.Checked)
            {
                TCPProtocolLabel.Enabled = true;
                TCPProtocolComboBox.Enabled = true;
                labelObfs.Enabled = true;
                ObfsCombo.Enabled = true;
                labelObfsParam.Enabled = true;
                TextObfsParam.Enabled = true;
            }
            else
            {
                TCPProtocolLabel.Enabled = false;
                TCPProtocolComboBox.Enabled = false;
                labelObfs.Enabled = false;
                ObfsCombo.Enabled = false;
                labelObfsParam.Enabled = false;
                TextObfsParam.Enabled = false;
            }
            if (CheckSSR.Checked && checkAdvSetting.Checked)
            {
                UDPoverTCPLabel.Visible = true;
                CheckUDPoverUDP.Visible = true;
            }
            else
            {
                UDPoverTCPLabel.Visible = false;
                CheckUDPoverUDP.Visible = false;
            }
            ResumeLayout();
        }

        private void CheckSSR_CheckedChanged(object sender, EventArgs e)
        {
            Update_SSR_controls_Visable();
        }
    }
}
