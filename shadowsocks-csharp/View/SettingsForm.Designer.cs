namespace Shadowsocks.View
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel10 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.MyCancelButton = new System.Windows.Forms.Button();
            this.OKButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.ProxyPortTextBox = new System.Windows.Forms.TextBox();
            this.ProxyPortLabel = new System.Windows.Forms.Label();
            this.ReconnectLabel = new System.Windows.Forms.Label();
            this.ReconnectText = new System.Windows.Forms.TextBox();
            this.TTLLabel = new System.Windows.Forms.Label();
            this.TTLText = new System.Windows.Forms.TextBox();
            this.checkShareOverLan = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.LabelRandom = new System.Windows.Forms.Label();
            this.RandomComboBox = new System.Windows.Forms.ComboBox();
            this.CheckAutoBan = new System.Windows.Forms.CheckBox();
            this.checkRandom = new System.Windows.Forms.CheckBox();
            this.checkAutoStartup = new System.Windows.Forms.CheckBox();
            this.Socks5ProxyGroup = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel9 = new System.Windows.Forms.TableLayoutPanel();
            this.TextS5Pass = new System.Windows.Forms.TextBox();
            this.TextS5User = new System.Windows.Forms.TextBox();
            this.LabelS5Password = new System.Windows.Forms.Label();
            this.LabelS5Server = new System.Windows.Forms.Label();
            this.LabelS5Port = new System.Windows.Forms.Label();
            this.TextS5Server = new System.Windows.Forms.TextBox();
            this.TextS5Port = new System.Windows.Forms.TextBox();
            this.LabelS5Username = new System.Windows.Forms.Label();
            this.CheckSocks5Proxy = new System.Windows.Forms.CheckBox();
            this.checkBuildinHttpProxy = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel10.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.Socks5ProxyGroup.SuspendLayout();
            this.tableLayoutPanel9.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel10, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.Socks5ProxyGroup, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(15, 15);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(576, 295);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel10
            // 
            this.tableLayoutPanel10.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.tableLayoutPanel10.AutoSize = true;
            this.tableLayoutPanel10.ColumnCount = 1;
            this.tableLayoutPanel10.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel10.Controls.Add(this.tableLayoutPanel3, 0, 2);
            this.tableLayoutPanel10.Controls.Add(this.tableLayoutPanel5, 0, 1);
            this.tableLayoutPanel10.Location = new System.Drawing.Point(340, 109);
            this.tableLayoutPanel10.Name = "tableLayoutPanel10";
            this.tableLayoutPanel10.RowCount = 3;
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel10.Size = new System.Drawing.Size(233, 183);
            this.tableLayoutPanel10.TabIndex = 20;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.tableLayoutPanel3.AutoSize = true;
            this.tableLayoutPanel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutPanel3.Controls.Add(this.MyCancelButton, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.OKButton, 0, 0);
            this.tableLayoutPanel3.Location = new System.Drawing.Point(26, 141);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.Size = new System.Drawing.Size(183, 39);
            this.tableLayoutPanel3.TabIndex = 14;
            // 
            // MyCancelButton
            // 
            this.MyCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.MyCancelButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.MyCancelButton.Location = new System.Drawing.Point(96, 3);
            this.MyCancelButton.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
            this.MyCancelButton.Name = "MyCancelButton";
            this.MyCancelButton.Size = new System.Drawing.Size(87, 36);
            this.MyCancelButton.TabIndex = 9;
            this.MyCancelButton.Text = "Cancel";
            this.MyCancelButton.UseVisualStyleBackColor = true;
            this.MyCancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // OKButton
            // 
            this.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.OKButton.Location = new System.Drawing.Point(3, 3);
            this.OKButton.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(87, 36);
            this.OKButton.TabIndex = 8;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.tableLayoutPanel5.AutoSize = true;
            this.tableLayoutPanel5.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel5.ColumnCount = 2;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel5.Controls.Add(this.ProxyPortTextBox, 1, 2);
            this.tableLayoutPanel5.Controls.Add(this.ProxyPortLabel, 0, 2);
            this.tableLayoutPanel5.Controls.Add(this.ReconnectLabel, 0, 3);
            this.tableLayoutPanel5.Controls.Add(this.ReconnectText, 1, 3);
            this.tableLayoutPanel5.Controls.Add(this.TTLLabel, 0, 4);
            this.tableLayoutPanel5.Controls.Add(this.TTLText, 1, 4);
            this.tableLayoutPanel5.Controls.Add(this.checkShareOverLan, 1, 1);
            this.tableLayoutPanel5.Controls.Add(this.checkBuildinHttpProxy, 1, 0);
            this.tableLayoutPanel5.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel5.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.Padding = new System.Windows.Forms.Padding(3);
            this.tableLayoutPanel5.RowCount = 5;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.Size = new System.Drawing.Size(233, 138);
            this.tableLayoutPanel5.TabIndex = 9;
            // 
            // ProxyPortTextBox
            // 
            this.ProxyPortTextBox.Location = new System.Drawing.Point(78, 54);
            this.ProxyPortTextBox.MaxLength = 10;
            this.ProxyPortTextBox.Name = "ProxyPortTextBox";
            this.ProxyPortTextBox.Size = new System.Drawing.Size(109, 22);
            this.ProxyPortTextBox.TabIndex = 4;
            this.ProxyPortTextBox.WordWrap = false;
            // 
            // ProxyPortLabel
            // 
            this.ProxyPortLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.ProxyPortLabel.AutoSize = true;
            this.ProxyPortLabel.Location = new System.Drawing.Point(8, 58);
            this.ProxyPortLabel.Name = "ProxyPortLabel";
            this.ProxyPortLabel.Size = new System.Drawing.Size(64, 14);
            this.ProxyPortLabel.TabIndex = 3;
            this.ProxyPortLabel.Text = "Proxy Port";
            // 
            // ReconnectLabel
            // 
            this.ReconnectLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.ReconnectLabel.AutoSize = true;
            this.ReconnectLabel.Location = new System.Drawing.Point(6, 86);
            this.ReconnectLabel.Name = "ReconnectLabel";
            this.ReconnectLabel.Size = new System.Drawing.Size(66, 14);
            this.ReconnectLabel.TabIndex = 3;
            this.ReconnectLabel.Text = "Reconnect";
            // 
            // ReconnectText
            // 
            this.ReconnectText.Location = new System.Drawing.Point(78, 82);
            this.ReconnectText.MaxLength = 10;
            this.ReconnectText.Name = "ReconnectText";
            this.ReconnectText.Size = new System.Drawing.Size(109, 22);
            this.ReconnectText.TabIndex = 4;
            this.ReconnectText.WordWrap = false;
            // 
            // TTLLabel
            // 
            this.TTLLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.TTLLabel.AutoSize = true;
            this.TTLLabel.Location = new System.Drawing.Point(43, 114);
            this.TTLLabel.Name = "TTLLabel";
            this.TTLLabel.Size = new System.Drawing.Size(29, 14);
            this.TTLLabel.TabIndex = 3;
            this.TTLLabel.Text = "TTL";
            // 
            // TTLText
            // 
            this.TTLText.Location = new System.Drawing.Point(78, 110);
            this.TTLText.MaxLength = 10;
            this.TTLText.Name = "TTLText";
            this.TTLText.Size = new System.Drawing.Size(109, 22);
            this.TTLText.TabIndex = 4;
            this.TTLText.WordWrap = false;
            // 
            // checkShareOverLan
            // 
            this.checkShareOverLan.AutoSize = true;
            this.checkShareOverLan.Location = new System.Drawing.Point(78, 30);
            this.checkShareOverLan.Name = "checkShareOverLan";
            this.checkShareOverLan.Size = new System.Drawing.Size(149, 18);
            this.checkShareOverLan.TabIndex = 5;
            this.checkShareOverLan.Text = "Allow Clients from LAN";
            this.checkShareOverLan.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.AutoSize = true;
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.LabelRandom, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.RandomComboBox, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.CheckAutoBan, 1, 3);
            this.tableLayoutPanel2.Controls.Add(this.checkRandom, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.checkAutoStartup, 1, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(340, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 4;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(169, 100);
            this.tableLayoutPanel2.TabIndex = 21;
            // 
            // LabelRandom
            // 
            this.LabelRandom.AutoSize = true;
            this.LabelRandom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LabelRandom.Location = new System.Drawing.Point(3, 48);
            this.LabelRandom.Name = "LabelRandom";
            this.LabelRandom.Size = new System.Drawing.Size(48, 28);
            this.LabelRandom.TabIndex = 12;
            this.LabelRandom.Text = "Balance";
            this.LabelRandom.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // RandomComboBox
            // 
            this.RandomComboBox.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.RandomComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RandomComboBox.FormattingEnabled = true;
            this.RandomComboBox.Items.AddRange(new object[] {
            "Order",
            "Random",
            "LowLatency",
            "LowException",
            "SelectedFirst",
            "Timer"});
            this.RandomComboBox.Location = new System.Drawing.Point(57, 51);
            this.RandomComboBox.Name = "RandomComboBox";
            this.RandomComboBox.Size = new System.Drawing.Size(109, 22);
            this.RandomComboBox.TabIndex = 13;
            // 
            // CheckAutoBan
            // 
            this.CheckAutoBan.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.CheckAutoBan.AutoSize = true;
            this.CheckAutoBan.Location = new System.Drawing.Point(75, 79);
            this.CheckAutoBan.Name = "CheckAutoBan";
            this.CheckAutoBan.Size = new System.Drawing.Size(73, 18);
            this.CheckAutoBan.TabIndex = 18;
            this.CheckAutoBan.Text = "AutoBan";
            this.CheckAutoBan.UseVisualStyleBackColor = true;
            // 
            // checkRandom
            // 
            this.checkRandom.AutoSize = true;
            this.checkRandom.Location = new System.Drawing.Point(57, 27);
            this.checkRandom.Name = "checkRandom";
            this.checkRandom.Size = new System.Drawing.Size(107, 18);
            this.checkRandom.TabIndex = 19;
            this.checkRandom.Text = "Enable balance";
            this.checkRandom.UseVisualStyleBackColor = true;
            // 
            // checkAutoStartup
            // 
            this.checkAutoStartup.AutoSize = true;
            this.checkAutoStartup.Location = new System.Drawing.Point(57, 3);
            this.checkAutoStartup.Name = "checkAutoStartup";
            this.checkAutoStartup.Size = new System.Drawing.Size(101, 18);
            this.checkAutoStartup.TabIndex = 20;
            this.checkAutoStartup.Text = "Start on Boot";
            this.checkAutoStartup.UseVisualStyleBackColor = true;
            // 
            // Socks5ProxyGroup
            // 
            this.Socks5ProxyGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Socks5ProxyGroup.AutoSize = true;
            this.Socks5ProxyGroup.Controls.Add(this.tableLayoutPanel9);
            this.Socks5ProxyGroup.Location = new System.Drawing.Point(14, 0);
            this.Socks5ProxyGroup.Margin = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.Socks5ProxyGroup.Name = "Socks5ProxyGroup";
            this.tableLayoutPanel1.SetRowSpan(this.Socks5ProxyGroup, 2);
            this.Socks5ProxyGroup.Size = new System.Drawing.Size(323, 185);
            this.Socks5ProxyGroup.TabIndex = 19;
            this.Socks5ProxyGroup.TabStop = false;
            this.Socks5ProxyGroup.Text = "Socks5 Proxy";
            // 
            // tableLayoutPanel9
            // 
            this.tableLayoutPanel9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel9.AutoSize = true;
            this.tableLayoutPanel9.ColumnCount = 2;
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 29.39189F));
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70.60811F));
            this.tableLayoutPanel9.Controls.Add(this.TextS5Pass, 1, 4);
            this.tableLayoutPanel9.Controls.Add(this.TextS5User, 1, 3);
            this.tableLayoutPanel9.Controls.Add(this.LabelS5Password, 0, 4);
            this.tableLayoutPanel9.Controls.Add(this.LabelS5Server, 0, 1);
            this.tableLayoutPanel9.Controls.Add(this.LabelS5Port, 0, 2);
            this.tableLayoutPanel9.Controls.Add(this.TextS5Server, 1, 1);
            this.tableLayoutPanel9.Controls.Add(this.TextS5Port, 1, 2);
            this.tableLayoutPanel9.Controls.Add(this.LabelS5Username, 0, 3);
            this.tableLayoutPanel9.Controls.Add(this.CheckSocks5Proxy, 1, 0);
            this.tableLayoutPanel9.Location = new System.Drawing.Point(-30, 24);
            this.tableLayoutPanel9.Name = "tableLayoutPanel9";
            this.tableLayoutPanel9.RowCount = 5;
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.Size = new System.Drawing.Size(350, 140);
            this.tableLayoutPanel9.TabIndex = 0;
            // 
            // TextS5Pass
            // 
            this.TextS5Pass.Location = new System.Drawing.Point(105, 111);
            this.TextS5Pass.Name = "TextS5Pass";
            this.TextS5Pass.Size = new System.Drawing.Size(236, 22);
            this.TextS5Pass.TabIndex = 7;
            // 
            // TextS5User
            // 
            this.TextS5User.Location = new System.Drawing.Point(105, 83);
            this.TextS5User.Name = "TextS5User";
            this.TextS5User.Size = new System.Drawing.Size(236, 22);
            this.TextS5User.TabIndex = 6;
            // 
            // LabelS5Password
            // 
            this.LabelS5Password.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelS5Password.AutoSize = true;
            this.LabelS5Password.Location = new System.Drawing.Point(41, 117);
            this.LabelS5Password.Name = "LabelS5Password";
            this.LabelS5Password.Size = new System.Drawing.Size(58, 14);
            this.LabelS5Password.TabIndex = 5;
            this.LabelS5Password.Text = "Password";
            // 
            // LabelS5Server
            // 
            this.LabelS5Server.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelS5Server.AutoSize = true;
            this.LabelS5Server.Location = new System.Drawing.Point(42, 31);
            this.LabelS5Server.Name = "LabelS5Server";
            this.LabelS5Server.Size = new System.Drawing.Size(57, 14);
            this.LabelS5Server.TabIndex = 0;
            this.LabelS5Server.Text = "Server IP";
            // 
            // LabelS5Port
            // 
            this.LabelS5Port.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelS5Port.AutoSize = true;
            this.LabelS5Port.Location = new System.Drawing.Point(69, 59);
            this.LabelS5Port.Name = "LabelS5Port";
            this.LabelS5Port.Size = new System.Drawing.Size(30, 14);
            this.LabelS5Port.TabIndex = 1;
            this.LabelS5Port.Text = "Port";
            // 
            // TextS5Server
            // 
            this.TextS5Server.Location = new System.Drawing.Point(105, 27);
            this.TextS5Server.Name = "TextS5Server";
            this.TextS5Server.Size = new System.Drawing.Size(236, 22);
            this.TextS5Server.TabIndex = 2;
            // 
            // TextS5Port
            // 
            this.TextS5Port.Location = new System.Drawing.Point(105, 55);
            this.TextS5Port.Name = "TextS5Port";
            this.TextS5Port.Size = new System.Drawing.Size(236, 22);
            this.TextS5Port.TabIndex = 3;
            // 
            // LabelS5Username
            // 
            this.LabelS5Username.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelS5Username.AutoSize = true;
            this.LabelS5Username.Location = new System.Drawing.Point(38, 87);
            this.LabelS5Username.Name = "LabelS5Username";
            this.LabelS5Username.Size = new System.Drawing.Size(61, 14);
            this.LabelS5Username.TabIndex = 4;
            this.LabelS5Username.Text = "Username";
            // 
            // CheckSocks5Proxy
            // 
            this.CheckSocks5Proxy.AutoSize = true;
            this.CheckSocks5Proxy.Location = new System.Drawing.Point(105, 3);
            this.CheckSocks5Proxy.Name = "CheckSocks5Proxy";
            this.CheckSocks5Proxy.Size = new System.Drawing.Size(118, 18);
            this.CheckSocks5Proxy.TabIndex = 8;
            this.CheckSocks5Proxy.Text = "Socks5 Proxy On";
            this.CheckSocks5Proxy.UseVisualStyleBackColor = true;
            // 
            // checkBuildinHttpProxy
            // 
            this.checkBuildinHttpProxy.AutoSize = true;
            this.checkBuildinHttpProxy.Location = new System.Drawing.Point(78, 6);
            this.checkBuildinHttpProxy.Name = "checkBuildinHttpProxy";
            this.checkBuildinHttpProxy.Size = new System.Drawing.Size(126, 18);
            this.checkBuildinHttpProxy.TabIndex = 6;
            this.checkBuildinHttpProxy.Text = "Build-in http proxy";
            this.checkBuildinHttpProxy.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(728, 409);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SettingsForm";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SettingsForm_FormClosed);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel10.ResumeLayout(false);
            this.tableLayoutPanel10.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.Socks5ProxyGroup.ResumeLayout(false);
            this.Socks5ProxyGroup.PerformLayout();
            this.tableLayoutPanel9.ResumeLayout(false);
            this.tableLayoutPanel9.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox Socks5ProxyGroup;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel9;
        private System.Windows.Forms.TextBox TextS5Pass;
        private System.Windows.Forms.TextBox TextS5User;
        private System.Windows.Forms.Label LabelS5Password;
        private System.Windows.Forms.Label LabelS5Server;
        private System.Windows.Forms.Label LabelS5Port;
        private System.Windows.Forms.TextBox TextS5Server;
        private System.Windows.Forms.TextBox TextS5Port;
        private System.Windows.Forms.Label LabelS5Username;
        private System.Windows.Forms.CheckBox CheckSocks5Proxy;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel10;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Button MyCancelButton;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.TextBox ProxyPortTextBox;
        private System.Windows.Forms.Label ProxyPortLabel;
        private System.Windows.Forms.Label ReconnectLabel;
        private System.Windows.Forms.TextBox ReconnectText;
        private System.Windows.Forms.Label TTLLabel;
        private System.Windows.Forms.TextBox TTLText;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label LabelRandom;
        private System.Windows.Forms.ComboBox RandomComboBox;
        private System.Windows.Forms.CheckBox CheckAutoBan;
        private System.Windows.Forms.CheckBox checkRandom;
        private System.Windows.Forms.CheckBox checkAutoStartup;
        private System.Windows.Forms.CheckBox checkShareOverLan;
        private System.Windows.Forms.CheckBox checkBuildinHttpProxy;
    }
}