namespace Shadowsocks.View
{
    partial class HotkeySettingsForm
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
            System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnRegisterAll = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.SwitchSystemProxyLabel = new System.Windows.Forms.Label();
            this.ChangeToPacLabel = new System.Windows.Forms.Label();
            this.ChangeToGlobalLabel = new System.Windows.Forms.Label();
            this.SwitchAllowLanLabel = new System.Windows.Forms.Label();
            this.ShowLogsLabel = new System.Windows.Forms.Label();
            this.ServerMoveUpLabel = new System.Windows.Forms.Label();
            this.ServerMoveDownLabel = new System.Windows.Forms.Label();
            this.SwitchSystemProxyTextBox = new System.Windows.Forms.TextBox();
            this.ChangeToPacTextBox = new System.Windows.Forms.TextBox();
            this.ChangeToGlobalTextBox = new System.Windows.Forms.TextBox();
            this.SwitchAllowLanTextBox = new System.Windows.Forms.TextBox();
            this.ShowLogsTextBox = new System.Windows.Forms.TextBox();
            this.ServerMoveUpTextBox = new System.Windows.Forms.TextBox();
            this.ServerMoveDownTextBox = new System.Windows.Forms.TextBox();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            flowLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.tableLayoutPanel1.SetColumnSpan(flowLayoutPanel1, 2);
            flowLayoutPanel1.Controls.Add(this.btnOK);
            flowLayoutPanel1.Controls.Add(this.btnCancel);
            flowLayoutPanel1.Controls.Add(this.btnRegisterAll);
            flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.BottomUp;
            flowLayoutPanel1.Location = new System.Drawing.Point(0, 227);
            flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 0, 16, 0);
            flowLayoutPanel1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            flowLayoutPanel1.Size = new System.Drawing.Size(475, 44);
            flowLayoutPanel1.TabIndex = 6;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(333, 10);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(123, 31);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(204, 10);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(123, 31);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // btnRegisterAll
            // 
            this.btnRegisterAll.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnRegisterAll.Location = new System.Drawing.Point(75, 10);
            this.btnRegisterAll.Name = "btnRegisterAll";
            this.btnRegisterAll.Size = new System.Drawing.Size(123, 31);
            this.btnRegisterAll.TabIndex = 2;
            this.btnRegisterAll.Text = "Reg All";
            this.btnRegisterAll.UseVisualStyleBackColor = true;
            this.btnRegisterAll.Click += new System.EventHandler(this.RegisterAllButton_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.SwitchSystemProxyLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.ChangeToPacLabel, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.ChangeToGlobalLabel, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.SwitchAllowLanLabel, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.ShowLogsLabel, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.ServerMoveUpLabel, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.ServerMoveDownLabel, 0, 6);
            this.tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.SwitchSystemProxyTextBox, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.ChangeToPacTextBox, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.ChangeToGlobalTextBox, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.SwitchAllowLanTextBox, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.ShowLogsTextBox, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.ServerMoveUpTextBox, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.ServerMoveDownTextBox, 1, 6);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 8;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.16667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.16667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.16667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.16667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.16667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.77778F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.38889F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(475, 271);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // SwitchSystemProxyLabel
            // 
            this.SwitchSystemProxyLabel.AutoSize = true;
            this.SwitchSystemProxyLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.SwitchSystemProxyLabel.Location = new System.Drawing.Point(25, 0);
            this.SwitchSystemProxyLabel.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.SwitchSystemProxyLabel.Name = "SwitchSystemProxyLabel";
            this.SwitchSystemProxyLabel.Size = new System.Drawing.Size(147, 32);
            this.SwitchSystemProxyLabel.TabIndex = 0;
            this.SwitchSystemProxyLabel.Text = "Enable System Proxy";
            this.SwitchSystemProxyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ChangeToPacLabel
            // 
            this.ChangeToPacLabel.AutoSize = true;
            this.ChangeToPacLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.ChangeToPacLabel.Location = new System.Drawing.Point(135, 32);
            this.ChangeToPacLabel.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.ChangeToPacLabel.Name = "ChangeToPacLabel";
            this.ChangeToPacLabel.Size = new System.Drawing.Size(37, 32);
            this.ChangeToPacLabel.TabIndex = 1;
            this.ChangeToPacLabel.Text = "PAC";
            this.ChangeToPacLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ChangeToGlobalLabel
            // 
            this.ChangeToGlobalLabel.AutoSize = true;
            this.ChangeToGlobalLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.ChangeToGlobalLabel.Location = new System.Drawing.Point(119, 64);
            this.ChangeToGlobalLabel.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.ChangeToGlobalLabel.Name = "ChangeToGlobalLabel";
            this.ChangeToGlobalLabel.Size = new System.Drawing.Size(53, 32);
            this.ChangeToGlobalLabel.TabIndex = 2;
            this.ChangeToGlobalLabel.Text = "Global";
            this.ChangeToGlobalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // SwitchAllowLanLabel
            // 
            this.SwitchAllowLanLabel.AutoSize = true;
            this.SwitchAllowLanLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.SwitchAllowLanLabel.Location = new System.Drawing.Point(8, 96);
            this.SwitchAllowLanLabel.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.SwitchAllowLanLabel.Name = "SwitchAllowLanLabel";
            this.SwitchAllowLanLabel.Size = new System.Drawing.Size(164, 32);
            this.SwitchAllowLanLabel.TabIndex = 3;
            this.SwitchAllowLanLabel.Text = "Allow Clients from LAN";
            this.SwitchAllowLanLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ShowLogsLabel
            // 
            this.ShowLogsLabel.AutoSize = true;
            this.ShowLogsLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.ShowLogsLabel.Location = new System.Drawing.Point(82, 128);
            this.ShowLogsLabel.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.ShowLogsLabel.Name = "ShowLogsLabel";
            this.ShowLogsLabel.Size = new System.Drawing.Size(90, 32);
            this.ShowLogsLabel.TabIndex = 4;
            this.ShowLogsLabel.Text = "Show Logs...";
            this.ShowLogsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ServerMoveUpLabel
            // 
            this.ServerMoveUpLabel.AutoSize = true;
            this.ServerMoveUpLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.ServerMoveUpLabel.Location = new System.Drawing.Point(103, 160);
            this.ServerMoveUpLabel.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.ServerMoveUpLabel.Name = "ServerMoveUpLabel";
            this.ServerMoveUpLabel.Size = new System.Drawing.Size(69, 34);
            this.ServerMoveUpLabel.TabIndex = 4;
            this.ServerMoveUpLabel.Text = "Move up";
            this.ServerMoveUpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ServerMoveDownLabel
            // 
            this.ServerMoveDownLabel.AutoSize = true;
            this.ServerMoveDownLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.ServerMoveDownLabel.Location = new System.Drawing.Point(81, 194);
            this.ServerMoveDownLabel.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.ServerMoveDownLabel.Name = "ServerMoveDownLabel";
            this.ServerMoveDownLabel.Size = new System.Drawing.Size(91, 33);
            this.ServerMoveDownLabel.TabIndex = 4;
            this.ServerMoveDownLabel.Text = "Move Down";
            this.ServerMoveDownLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // SwitchSystemProxyTextBox
            // 
            this.SwitchSystemProxyTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SwitchSystemProxyTextBox.Location = new System.Drawing.Point(183, 3);
            this.SwitchSystemProxyTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
            this.SwitchSystemProxyTextBox.Name = "SwitchSystemProxyTextBox";
            this.SwitchSystemProxyTextBox.ReadOnly = true;
            this.SwitchSystemProxyTextBox.Size = new System.Drawing.Size(276, 25);
            this.SwitchSystemProxyTextBox.TabIndex = 7;
            this.SwitchSystemProxyTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            this.SwitchSystemProxyTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HotkeyDown);
            this.SwitchSystemProxyTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.HotkeyUp);
            // 
            // ChangeToPacTextBox
            // 
            this.ChangeToPacTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChangeToPacTextBox.Location = new System.Drawing.Point(183, 35);
            this.ChangeToPacTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
            this.ChangeToPacTextBox.Name = "ChangeToPacTextBox";
            this.ChangeToPacTextBox.ReadOnly = true;
            this.ChangeToPacTextBox.Size = new System.Drawing.Size(276, 25);
            this.ChangeToPacTextBox.TabIndex = 8;
            this.ChangeToPacTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            this.ChangeToPacTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HotkeyDown);
            this.ChangeToPacTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.HotkeyUp);
            // 
            // ChangeToGlobalTextBox
            // 
            this.ChangeToGlobalTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChangeToGlobalTextBox.Location = new System.Drawing.Point(183, 67);
            this.ChangeToGlobalTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
            this.ChangeToGlobalTextBox.Name = "ChangeToGlobalTextBox";
            this.ChangeToGlobalTextBox.ReadOnly = true;
            this.ChangeToGlobalTextBox.Size = new System.Drawing.Size(276, 25);
            this.ChangeToGlobalTextBox.TabIndex = 9;
            this.ChangeToGlobalTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            this.ChangeToGlobalTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HotkeyDown);
            this.ChangeToGlobalTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.HotkeyUp);
            // 
            // SwitchAllowLanTextBox
            // 
            this.SwitchAllowLanTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SwitchAllowLanTextBox.Location = new System.Drawing.Point(183, 99);
            this.SwitchAllowLanTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
            this.SwitchAllowLanTextBox.Name = "SwitchAllowLanTextBox";
            this.SwitchAllowLanTextBox.ReadOnly = true;
            this.SwitchAllowLanTextBox.Size = new System.Drawing.Size(276, 25);
            this.SwitchAllowLanTextBox.TabIndex = 10;
            this.SwitchAllowLanTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            this.SwitchAllowLanTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HotkeyDown);
            this.SwitchAllowLanTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.HotkeyUp);
            // 
            // ShowLogsTextBox
            // 
            this.ShowLogsTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ShowLogsTextBox.Location = new System.Drawing.Point(183, 131);
            this.ShowLogsTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
            this.ShowLogsTextBox.Name = "ShowLogsTextBox";
            this.ShowLogsTextBox.ReadOnly = true;
            this.ShowLogsTextBox.Size = new System.Drawing.Size(276, 25);
            this.ShowLogsTextBox.TabIndex = 11;
            this.ShowLogsTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            this.ShowLogsTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HotkeyDown);
            this.ShowLogsTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.HotkeyUp);
            // 
            // ServerMoveUpTextBox
            // 
            this.ServerMoveUpTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ServerMoveUpTextBox.Location = new System.Drawing.Point(183, 163);
            this.ServerMoveUpTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
            this.ServerMoveUpTextBox.Name = "ServerMoveUpTextBox";
            this.ServerMoveUpTextBox.ReadOnly = true;
            this.ServerMoveUpTextBox.Size = new System.Drawing.Size(276, 25);
            this.ServerMoveUpTextBox.TabIndex = 12;
            this.ServerMoveUpTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            this.ServerMoveUpTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HotkeyDown);
            this.ServerMoveUpTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.HotkeyUp);
            // 
            // ServerMoveDownTextBox
            // 
            this.ServerMoveDownTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ServerMoveDownTextBox.Location = new System.Drawing.Point(183, 197);
            this.ServerMoveDownTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
            this.ServerMoveDownTextBox.Name = "ServerMoveDownTextBox";
            this.ServerMoveDownTextBox.ReadOnly = true;
            this.ServerMoveDownTextBox.Size = new System.Drawing.Size(276, 25);
            this.ServerMoveDownTextBox.TabIndex = 13;
            this.ServerMoveDownTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            this.ServerMoveDownTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HotkeyDown);
            this.ServerMoveDownTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.HotkeyUp);
            // 
            // HotkeySettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(475, 271);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HotkeySettingsForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Edit Hotkeys...";
            flowLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label SwitchSystemProxyLabel;
        private System.Windows.Forms.Label ChangeToPacLabel;
        private System.Windows.Forms.Label ChangeToGlobalLabel;
        private System.Windows.Forms.Label SwitchAllowLanLabel;
        private System.Windows.Forms.Label ShowLogsLabel;
        private System.Windows.Forms.Label ServerMoveUpLabel;
        private System.Windows.Forms.Label ServerMoveDownLabel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox ShowLogsTextBox;
        private System.Windows.Forms.TextBox SwitchAllowLanTextBox;
        private System.Windows.Forms.TextBox ChangeToGlobalTextBox;
        private System.Windows.Forms.TextBox ChangeToPacTextBox;
        private System.Windows.Forms.TextBox SwitchSystemProxyTextBox;
        private System.Windows.Forms.TextBox ServerMoveUpTextBox;
        private System.Windows.Forms.TextBox ServerMoveDownTextBox;
        private System.Windows.Forms.Button btnRegisterAll;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}