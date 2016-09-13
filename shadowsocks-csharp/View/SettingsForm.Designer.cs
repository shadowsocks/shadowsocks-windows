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
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.LabelRandom = new System.Windows.Forms.Label();
            this.RandomComboBox = new System.Windows.Forms.ComboBox();
            this.CheckAutoBan = new System.Windows.Forms.CheckBox();
            this.checkRandom = new System.Windows.Forms.CheckBox();
            this.checkAutoStartup = new System.Windows.Forms.CheckBox();
            this.Socks5ProxyGroup = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel9 = new System.Windows.Forms.TableLayoutPanel();
            this.LabelS5Password = new System.Windows.Forms.Label();
            this.LabelS5Username = new System.Windows.Forms.Label();
            this.TextS5Pass = new System.Windows.Forms.TextBox();
            this.LabelS5Port = new System.Windows.Forms.Label();
            this.TextS5User = new System.Windows.Forms.TextBox();
            this.LabelS5Server = new System.Windows.Forms.Label();
            this.TextS5Port = new System.Windows.Forms.TextBox();
            this.TextS5Server = new System.Windows.Forms.TextBox();
            this.comboProxyType = new System.Windows.Forms.ComboBox();
            this.CheckSockProxy = new System.Windows.Forms.CheckBox();
            this.checkBoxPacProxy = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.TextUserAgent = new System.Windows.Forms.TextBox();
            this.ListenGroup = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.TextAuthPass = new System.Windows.Forms.TextBox();
            this.LabelAuthPass = new System.Windows.Forms.Label();
            this.TextAuthUser = new System.Windows.Forms.TextBox();
            this.LabelAuthUser = new System.Windows.Forms.Label();
            this.checkShareOverLan = new System.Windows.Forms.CheckBox();
            this.ProxyPortTextBox = new System.Windows.Forms.TextBox();
            this.ProxyPortLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel10 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.MyCancelButton = new System.Windows.Forms.Button();
            this.OKButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.ReconnectLabel = new System.Windows.Forms.Label();
            this.ReconnectText = new System.Windows.Forms.TextBox();
            this.TTLLabel = new System.Windows.Forms.Label();
            this.TTLText = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.DNSText = new System.Windows.Forms.TextBox();
            this.labelTimeout = new System.Windows.Forms.Label();
            this.textTimeout = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.Socks5ProxyGroup.SuspendLayout();
            this.tableLayoutPanel9.SuspendLayout();
            this.ListenGroup.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.tableLayoutPanel10.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.Socks5ProxyGroup, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.ListenGroup, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel10, 1, 2);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(15, 16);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(587, 435);
            this.tableLayoutPanel1.TabIndex = 0;
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
            this.tableLayoutPanel2.Location = new System.Drawing.Point(372, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 4;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(212, 112);
            this.tableLayoutPanel2.TabIndex = 21;
            // 
            // LabelRandom
            // 
            this.LabelRandom.AutoSize = true;
            this.LabelRandom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LabelRandom.Location = new System.Drawing.Point(3, 54);
            this.LabelRandom.Name = "LabelRandom";
            this.LabelRandom.Size = new System.Drawing.Size(53, 31);
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
            "Timer",
            "SeletedGroup"});
            this.RandomComboBox.Location = new System.Drawing.Point(62, 57);
            this.RandomComboBox.Name = "RandomComboBox";
            this.RandomComboBox.Size = new System.Drawing.Size(147, 25);
            this.RandomComboBox.TabIndex = 13;
            // 
            // CheckAutoBan
            // 
            this.CheckAutoBan.AutoSize = true;
            this.CheckAutoBan.Location = new System.Drawing.Point(62, 88);
            this.CheckAutoBan.Name = "CheckAutoBan";
            this.CheckAutoBan.Size = new System.Drawing.Size(76, 21);
            this.CheckAutoBan.TabIndex = 18;
            this.CheckAutoBan.Text = "AutoBan";
            this.CheckAutoBan.UseVisualStyleBackColor = true;
            // 
            // checkRandom
            // 
            this.checkRandom.AutoSize = true;
            this.checkRandom.Location = new System.Drawing.Point(62, 30);
            this.checkRandom.Name = "checkRandom";
            this.checkRandom.Size = new System.Drawing.Size(115, 21);
            this.checkRandom.TabIndex = 19;
            this.checkRandom.Text = "Enable balance";
            this.checkRandom.UseVisualStyleBackColor = true;
            // 
            // checkAutoStartup
            // 
            this.checkAutoStartup.AutoSize = true;
            this.checkAutoStartup.Location = new System.Drawing.Point(62, 3);
            this.checkAutoStartup.Name = "checkAutoStartup";
            this.checkAutoStartup.Size = new System.Drawing.Size(105, 21);
            this.checkAutoStartup.TabIndex = 20;
            this.checkAutoStartup.Text = "Start on Boot";
            this.checkAutoStartup.UseVisualStyleBackColor = true;
            // 
            // Socks5ProxyGroup
            // 
            this.Socks5ProxyGroup.AutoSize = true;
            this.Socks5ProxyGroup.Controls.Add(this.tableLayoutPanel9);
            this.Socks5ProxyGroup.Location = new System.Drawing.Point(14, 0);
            this.Socks5ProxyGroup.Margin = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.Socks5ProxyGroup.Name = "Socks5ProxyGroup";
            this.tableLayoutPanel1.SetRowSpan(this.Socks5ProxyGroup, 2);
            this.Socks5ProxyGroup.Size = new System.Drawing.Size(355, 257);
            this.Socks5ProxyGroup.TabIndex = 19;
            this.Socks5ProxyGroup.TabStop = false;
            this.Socks5ProxyGroup.Text = "Remote proxy";
            // 
            // tableLayoutPanel9
            // 
            this.tableLayoutPanel9.AutoSize = true;
            this.tableLayoutPanel9.ColumnCount = 2;
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel9.Controls.Add(this.LabelS5Password, 0, 5);
            this.tableLayoutPanel9.Controls.Add(this.LabelS5Username, 0, 4);
            this.tableLayoutPanel9.Controls.Add(this.TextS5Pass, 1, 5);
            this.tableLayoutPanel9.Controls.Add(this.LabelS5Port, 0, 3);
            this.tableLayoutPanel9.Controls.Add(this.TextS5User, 1, 4);
            this.tableLayoutPanel9.Controls.Add(this.LabelS5Server, 0, 2);
            this.tableLayoutPanel9.Controls.Add(this.TextS5Port, 1, 3);
            this.tableLayoutPanel9.Controls.Add(this.TextS5Server, 1, 2);
            this.tableLayoutPanel9.Controls.Add(this.comboProxyType, 1, 1);
            this.tableLayoutPanel9.Controls.Add(this.CheckSockProxy, 0, 0);
            this.tableLayoutPanel9.Controls.Add(this.checkBoxPacProxy, 1, 0);
            this.tableLayoutPanel9.Controls.Add(this.label1, 0, 6);
            this.tableLayoutPanel9.Controls.Add(this.TextUserAgent, 1, 6);
            this.tableLayoutPanel9.Location = new System.Drawing.Point(11, 32);
            this.tableLayoutPanel9.Name = "tableLayoutPanel9";
            this.tableLayoutPanel9.RowCount = 7;
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.Size = new System.Drawing.Size(338, 203);
            this.tableLayoutPanel9.TabIndex = 0;
            // 
            // LabelS5Password
            // 
            this.LabelS5Password.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelS5Password.AutoSize = true;
            this.LabelS5Password.Location = new System.Drawing.Point(19, 151);
            this.LabelS5Password.Name = "LabelS5Password";
            this.LabelS5Password.Size = new System.Drawing.Size(64, 17);
            this.LabelS5Password.TabIndex = 5;
            this.LabelS5Password.Text = "Password";
            // 
            // LabelS5Username
            // 
            this.LabelS5Username.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelS5Username.AutoSize = true;
            this.LabelS5Username.Location = new System.Drawing.Point(16, 122);
            this.LabelS5Username.Name = "LabelS5Username";
            this.LabelS5Username.Size = new System.Drawing.Size(67, 17);
            this.LabelS5Username.TabIndex = 4;
            this.LabelS5Username.Text = "Username";
            // 
            // TextS5Pass
            // 
            this.TextS5Pass.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.TextS5Pass.Location = new System.Drawing.Point(89, 148);
            this.TextS5Pass.Name = "TextS5Pass";
            this.TextS5Pass.Size = new System.Drawing.Size(236, 23);
            this.TextS5Pass.TabIndex = 7;
            // 
            // LabelS5Port
            // 
            this.LabelS5Port.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelS5Port.AutoSize = true;
            this.LabelS5Port.Location = new System.Drawing.Point(51, 93);
            this.LabelS5Port.Name = "LabelS5Port";
            this.LabelS5Port.Size = new System.Drawing.Size(32, 17);
            this.LabelS5Port.TabIndex = 1;
            this.LabelS5Port.Text = "Port";
            // 
            // TextS5User
            // 
            this.TextS5User.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.TextS5User.Location = new System.Drawing.Point(89, 119);
            this.TextS5User.Name = "TextS5User";
            this.TextS5User.Size = new System.Drawing.Size(236, 23);
            this.TextS5User.TabIndex = 6;
            // 
            // LabelS5Server
            // 
            this.LabelS5Server.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelS5Server.AutoSize = true;
            this.LabelS5Server.Location = new System.Drawing.Point(23, 64);
            this.LabelS5Server.Name = "LabelS5Server";
            this.LabelS5Server.Size = new System.Drawing.Size(60, 17);
            this.LabelS5Server.TabIndex = 0;
            this.LabelS5Server.Text = "Server IP";
            // 
            // TextS5Port
            // 
            this.TextS5Port.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.TextS5Port.Location = new System.Drawing.Point(89, 90);
            this.TextS5Port.Name = "TextS5Port";
            this.TextS5Port.Size = new System.Drawing.Size(236, 23);
            this.TextS5Port.TabIndex = 3;
            // 
            // TextS5Server
            // 
            this.TextS5Server.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.TextS5Server.Location = new System.Drawing.Point(89, 61);
            this.TextS5Server.Name = "TextS5Server";
            this.TextS5Server.Size = new System.Drawing.Size(236, 23);
            this.TextS5Server.TabIndex = 2;
            // 
            // comboProxyType
            // 
            this.comboProxyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboProxyType.FormattingEnabled = true;
            this.comboProxyType.Items.AddRange(new object[] {
            "Socks5(support UDP)",
            "Http tunnel",
            "TCP Port tunnel"});
            this.comboProxyType.Location = new System.Drawing.Point(89, 30);
            this.comboProxyType.Name = "comboProxyType";
            this.comboProxyType.Size = new System.Drawing.Size(236, 25);
            this.comboProxyType.TabIndex = 9;
            // 
            // CheckSockProxy
            // 
            this.CheckSockProxy.AutoSize = true;
            this.CheckSockProxy.Location = new System.Drawing.Point(3, 3);
            this.CheckSockProxy.Name = "CheckSockProxy";
            this.CheckSockProxy.Size = new System.Drawing.Size(80, 21);
            this.CheckSockProxy.TabIndex = 8;
            this.CheckSockProxy.Text = "Proxy On";
            this.CheckSockProxy.UseVisualStyleBackColor = true;
            // 
            // checkBoxPacProxy
            // 
            this.checkBoxPacProxy.AutoSize = true;
            this.checkBoxPacProxy.Location = new System.Drawing.Point(89, 3);
            this.checkBoxPacProxy.Name = "checkBoxPacProxy";
            this.checkBoxPacProxy.Size = new System.Drawing.Size(197, 21);
            this.checkBoxPacProxy.TabIndex = 10;
            this.checkBoxPacProxy.Text = "PAC \"direct\" return this proxy";
            this.checkBoxPacProxy.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 180);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 17);
            this.label1.TabIndex = 5;
            this.label1.Text = "UserAgent";
            // 
            // TextUserAgent
            // 
            this.TextUserAgent.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.TextUserAgent.Location = new System.Drawing.Point(89, 177);
            this.TextUserAgent.Name = "TextUserAgent";
            this.TextUserAgent.Size = new System.Drawing.Size(236, 23);
            this.TextUserAgent.TabIndex = 7;
            // 
            // ListenGroup
            // 
            this.ListenGroup.AutoSize = true;
            this.ListenGroup.Controls.Add(this.tableLayoutPanel4);
            this.ListenGroup.Location = new System.Drawing.Point(14, 257);
            this.ListenGroup.Margin = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.ListenGroup.Name = "ListenGroup";
            this.ListenGroup.Size = new System.Drawing.Size(339, 178);
            this.ListenGroup.TabIndex = 22;
            this.ListenGroup.TabStop = false;
            this.ListenGroup.Text = "Local proxy";
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.AutoSize = true;
            this.tableLayoutPanel4.ColumnCount = 2;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel4.Controls.Add(this.TextAuthPass, 1, 3);
            this.tableLayoutPanel4.Controls.Add(this.LabelAuthPass, 0, 3);
            this.tableLayoutPanel4.Controls.Add(this.TextAuthUser, 1, 2);
            this.tableLayoutPanel4.Controls.Add(this.LabelAuthUser, 0, 2);
            this.tableLayoutPanel4.Controls.Add(this.checkShareOverLan, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.ProxyPortTextBox, 1, 1);
            this.tableLayoutPanel4.Controls.Add(this.ProxyPortLabel, 0, 1);
            this.tableLayoutPanel4.Location = new System.Drawing.Point(5, 32);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 4;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.Size = new System.Drawing.Size(328, 124);
            this.tableLayoutPanel4.TabIndex = 0;
            // 
            // TextAuthPass
            // 
            this.TextAuthPass.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.TextAuthPass.Location = new System.Drawing.Point(76, 88);
            this.TextAuthPass.Name = "TextAuthPass";
            this.TextAuthPass.Size = new System.Drawing.Size(236, 23);
            this.TextAuthPass.TabIndex = 9;
            // 
            // LabelAuthPass
            // 
            this.LabelAuthPass.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelAuthPass.AutoSize = true;
            this.LabelAuthPass.Location = new System.Drawing.Point(6, 96);
            this.LabelAuthPass.Name = "LabelAuthPass";
            this.LabelAuthPass.Size = new System.Drawing.Size(64, 17);
            this.LabelAuthPass.TabIndex = 8;
            this.LabelAuthPass.Text = "Password";
            // 
            // TextAuthUser
            // 
            this.TextAuthUser.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.TextAuthUser.Location = new System.Drawing.Point(76, 59);
            this.TextAuthUser.Name = "TextAuthUser";
            this.TextAuthUser.Size = new System.Drawing.Size(236, 23);
            this.TextAuthUser.TabIndex = 7;
            // 
            // LabelAuthUser
            // 
            this.LabelAuthUser.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelAuthUser.AutoSize = true;
            this.LabelAuthUser.Location = new System.Drawing.Point(3, 62);
            this.LabelAuthUser.Name = "LabelAuthUser";
            this.LabelAuthUser.Size = new System.Drawing.Size(67, 17);
            this.LabelAuthUser.TabIndex = 5;
            this.LabelAuthUser.Text = "Username";
            // 
            // checkShareOverLan
            // 
            this.checkShareOverLan.AutoSize = true;
            this.checkShareOverLan.Location = new System.Drawing.Point(76, 3);
            this.checkShareOverLan.Name = "checkShareOverLan";
            this.checkShareOverLan.Size = new System.Drawing.Size(160, 21);
            this.checkShareOverLan.TabIndex = 5;
            this.checkShareOverLan.Text = "Allow Clients from LAN";
            this.checkShareOverLan.UseVisualStyleBackColor = true;
            // 
            // ProxyPortTextBox
            // 
            this.ProxyPortTextBox.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.ProxyPortTextBox.Location = new System.Drawing.Point(76, 30);
            this.ProxyPortTextBox.MaxLength = 10;
            this.ProxyPortTextBox.Name = "ProxyPortTextBox";
            this.ProxyPortTextBox.Size = new System.Drawing.Size(236, 23);
            this.ProxyPortTextBox.TabIndex = 4;
            this.ProxyPortTextBox.WordWrap = false;
            // 
            // ProxyPortLabel
            // 
            this.ProxyPortLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.ProxyPortLabel.AutoSize = true;
            this.ProxyPortLabel.Location = new System.Drawing.Point(38, 33);
            this.ProxyPortLabel.Name = "ProxyPortLabel";
            this.ProxyPortLabel.Size = new System.Drawing.Size(32, 17);
            this.ProxyPortLabel.TabIndex = 3;
            this.ProxyPortLabel.Text = "Port";
            // 
            // tableLayoutPanel10
            // 
            this.tableLayoutPanel10.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.tableLayoutPanel10.AutoSize = true;
            this.tableLayoutPanel10.ColumnCount = 1;
            this.tableLayoutPanel10.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel10.Controls.Add(this.tableLayoutPanel3, 0, 2);
            this.tableLayoutPanel10.Controls.Add(this.tableLayoutPanel5, 0, 1);
            this.tableLayoutPanel10.Location = new System.Drawing.Point(380, 261);
            this.tableLayoutPanel10.Name = "tableLayoutPanel10";
            this.tableLayoutPanel10.RowCount = 3;
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel10.Size = new System.Drawing.Size(195, 170);
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
            this.tableLayoutPanel3.Location = new System.Drawing.Point(7, 125);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.Size = new System.Drawing.Size(183, 42);
            this.tableLayoutPanel3.TabIndex = 14;
            // 
            // MyCancelButton
            // 
            this.MyCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.MyCancelButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.MyCancelButton.Location = new System.Drawing.Point(96, 3);
            this.MyCancelButton.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
            this.MyCancelButton.Name = "MyCancelButton";
            this.MyCancelButton.Size = new System.Drawing.Size(87, 39);
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
            this.OKButton.Size = new System.Drawing.Size(87, 39);
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
            this.tableLayoutPanel5.Controls.Add(this.ReconnectLabel, 0, 3);
            this.tableLayoutPanel5.Controls.Add(this.ReconnectText, 1, 3);
            this.tableLayoutPanel5.Controls.Add(this.TTLLabel, 0, 5);
            this.tableLayoutPanel5.Controls.Add(this.TTLText, 1, 5);
            this.tableLayoutPanel5.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.DNSText, 1, 0);
            this.tableLayoutPanel5.Controls.Add(this.labelTimeout, 0, 4);
            this.tableLayoutPanel5.Controls.Add(this.textTimeout, 1, 4);
            this.tableLayoutPanel5.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel5.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.Padding = new System.Windows.Forms.Padding(3);
            this.tableLayoutPanel5.RowCount = 6;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(195, 122);
            this.tableLayoutPanel5.TabIndex = 9;
            // 
            // ReconnectLabel
            // 
            this.ReconnectLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.ReconnectLabel.AutoSize = true;
            this.ReconnectLabel.Location = new System.Drawing.Point(6, 38);
            this.ReconnectLabel.Name = "ReconnectLabel";
            this.ReconnectLabel.Size = new System.Drawing.Size(68, 17);
            this.ReconnectLabel.TabIndex = 3;
            this.ReconnectLabel.Text = "Reconnect";
            // 
            // ReconnectText
            // 
            this.ReconnectText.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.ReconnectText.Location = new System.Drawing.Point(80, 35);
            this.ReconnectText.MaxLength = 10;
            this.ReconnectText.Name = "ReconnectText";
            this.ReconnectText.Size = new System.Drawing.Size(109, 23);
            this.ReconnectText.TabIndex = 4;
            this.ReconnectText.WordWrap = false;
            // 
            // TTLLabel
            // 
            this.TTLLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.TTLLabel.AutoSize = true;
            this.TTLLabel.Location = new System.Drawing.Point(46, 96);
            this.TTLLabel.Name = "TTLLabel";
            this.TTLLabel.Size = new System.Drawing.Size(28, 17);
            this.TTLLabel.TabIndex = 3;
            this.TTLLabel.Text = "TTL";
            // 
            // TTLText
            // 
            this.TTLText.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.TTLText.Location = new System.Drawing.Point(80, 93);
            this.TTLText.MaxLength = 10;
            this.TTLText.Name = "TTLText";
            this.TTLText.Size = new System.Drawing.Size(109, 23);
            this.TTLText.TabIndex = 4;
            this.TTLText.WordWrap = false;
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(40, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "DNS";
            // 
            // DNSText
            // 
            this.DNSText.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.DNSText.Location = new System.Drawing.Point(80, 6);
            this.DNSText.MaxLength = 0;
            this.DNSText.Name = "DNSText";
            this.DNSText.Size = new System.Drawing.Size(109, 23);
            this.DNSText.TabIndex = 4;
            this.DNSText.WordWrap = false;
            // 
            // labelTimeout
            // 
            this.labelTimeout.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.labelTimeout.AutoSize = true;
            this.labelTimeout.Location = new System.Drawing.Point(15, 67);
            this.labelTimeout.Name = "labelTimeout";
            this.labelTimeout.Size = new System.Drawing.Size(59, 17);
            this.labelTimeout.TabIndex = 3;
            this.labelTimeout.Text = " Timeout";
            // 
            // textTimeout
            // 
            this.textTimeout.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.textTimeout.Location = new System.Drawing.Point(80, 64);
            this.textTimeout.MaxLength = 10;
            this.textTimeout.Name = "textTimeout";
            this.textTimeout.Size = new System.Drawing.Size(109, 23);
            this.textTimeout.TabIndex = 4;
            this.textTimeout.WordWrap = false;
            // 
            // SettingsForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(728, 573);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.Padding = new System.Windows.Forms.Padding(12, 13, 12, 13);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SettingsForm";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SettingsForm_FormClosed);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.Socks5ProxyGroup.ResumeLayout(false);
            this.Socks5ProxyGroup.PerformLayout();
            this.tableLayoutPanel9.ResumeLayout(false);
            this.tableLayoutPanel9.PerformLayout();
            this.ListenGroup.ResumeLayout(false);
            this.ListenGroup.PerformLayout();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.tableLayoutPanel10.ResumeLayout(false);
            this.tableLayoutPanel10.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
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
        private System.Windows.Forms.CheckBox CheckSockProxy;
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
        private System.Windows.Forms.ComboBox comboProxyType;
        private System.Windows.Forms.GroupBox ListenGroup;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.TextBox TextAuthPass;
        private System.Windows.Forms.Label LabelAuthPass;
        private System.Windows.Forms.TextBox TextAuthUser;
        private System.Windows.Forms.Label LabelAuthUser;
        private System.Windows.Forms.CheckBox checkBoxPacProxy;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox TextUserAgent;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox DNSText;
        private System.Windows.Forms.Label labelTimeout;
        private System.Windows.Forms.TextBox textTimeout;
    }
}