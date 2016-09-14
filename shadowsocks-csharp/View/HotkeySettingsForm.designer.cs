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
            System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
            System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
            this.lblSwitchSystemProxy = new System.Windows.Forms.Label();
            this.lblChangeToPac = new System.Windows.Forms.Label();
            this.lblChangeToGlobal = new System.Windows.Forms.Label();
            this.lblSwitchAllowLan = new System.Windows.Forms.Label();
            this.lblShowLogs = new System.Windows.Forms.Label();
            this.ok = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.txtSwitchSystemProxy = new System.Windows.Forms.TextBox();
            this.txtChangeToPac = new System.Windows.Forms.TextBox();
            this.txtChangeToGlobal = new System.Windows.Forms.TextBox();
            this.txtSwitchAllowLan = new System.Windows.Forms.TextBox();
            this.txtShowLogs = new System.Windows.Forms.TextBox();
            this.ckbAllowSwitchServer = new System.Windows.Forms.CheckBox();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            tableLayoutPanel1.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            tableLayoutPanel1.Controls.Add(this.lblSwitchSystemProxy, 0, 0);
            tableLayoutPanel1.Controls.Add(this.lblChangeToPac, 0, 1);
            tableLayoutPanel1.Controls.Add(this.lblChangeToGlobal, 0, 2);
            tableLayoutPanel1.Controls.Add(this.lblSwitchAllowLan, 0, 3);
            tableLayoutPanel1.Controls.Add(this.lblShowLogs, 0, 4);
            tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 6);
            tableLayoutPanel1.Controls.Add(this.txtSwitchSystemProxy, 1, 0);
            tableLayoutPanel1.Controls.Add(this.txtChangeToPac, 1, 1);
            tableLayoutPanel1.Controls.Add(this.txtChangeToGlobal, 1, 2);
            tableLayoutPanel1.Controls.Add(this.txtSwitchAllowLan, 1, 3);
            tableLayoutPanel1.Controls.Add(this.txtShowLogs, 1, 4);
            tableLayoutPanel1.Controls.Add(this.ckbAllowSwitchServer, 0, 5);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 7;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            tableLayoutPanel1.Size = new System.Drawing.Size(363, 250);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // lblSwitchSystemProxy
            // 
            this.lblSwitchSystemProxy.AutoSize = true;
            this.lblSwitchSystemProxy.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblSwitchSystemProxy.Location = new System.Drawing.Point(25, 0);
            this.lblSwitchSystemProxy.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblSwitchSystemProxy.Name = "lblSwitchSystemProxy";
            this.lblSwitchSystemProxy.Size = new System.Drawing.Size(147, 35);
            this.lblSwitchSystemProxy.TabIndex = 0;
            this.lblSwitchSystemProxy.Text = "Enable System Proxy";
            this.lblSwitchSystemProxy.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblChangeToPac
            // 
            this.lblChangeToPac.AutoSize = true;
            this.lblChangeToPac.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblChangeToPac.Location = new System.Drawing.Point(135, 35);
            this.lblChangeToPac.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblChangeToPac.Name = "lblChangeToPac";
            this.lblChangeToPac.Size = new System.Drawing.Size(37, 35);
            this.lblChangeToPac.TabIndex = 1;
            this.lblChangeToPac.Text = "PAC";
            this.lblChangeToPac.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblChangeToGlobal
            // 
            this.lblChangeToGlobal.AutoSize = true;
            this.lblChangeToGlobal.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblChangeToGlobal.Location = new System.Drawing.Point(119, 70);
            this.lblChangeToGlobal.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblChangeToGlobal.Name = "lblChangeToGlobal";
            this.lblChangeToGlobal.Size = new System.Drawing.Size(53, 35);
            this.lblChangeToGlobal.TabIndex = 2;
            this.lblChangeToGlobal.Text = "Global";
            this.lblChangeToGlobal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSwitchAllowLan
            // 
            this.lblSwitchAllowLan.AutoSize = true;
            this.lblSwitchAllowLan.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblSwitchAllowLan.Location = new System.Drawing.Point(8, 105);
            this.lblSwitchAllowLan.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblSwitchAllowLan.Name = "lblSwitchAllowLan";
            this.lblSwitchAllowLan.Size = new System.Drawing.Size(164, 35);
            this.lblSwitchAllowLan.TabIndex = 3;
            this.lblSwitchAllowLan.Text = "Allow Clients from LAN";
            this.lblSwitchAllowLan.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblShowLogs
            // 
            this.lblShowLogs.AutoSize = true;
            this.lblShowLogs.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblShowLogs.Location = new System.Drawing.Point(82, 140);
            this.lblShowLogs.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblShowLogs.Name = "lblShowLogs";
            this.lblShowLogs.Size = new System.Drawing.Size(90, 35);
            this.lblShowLogs.TabIndex = 4;
            this.lblShowLogs.Text = "Show Logs...";
            this.lblShowLogs.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanel1
            // 
            tableLayoutPanel1.SetColumnSpan(flowLayoutPanel1, 2);
            flowLayoutPanel1.Controls.Add(this.ok);
            flowLayoutPanel1.Controls.Add(this.cancel);
            flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.BottomUp;
            flowLayoutPanel1.Location = new System.Drawing.Point(0, 210);
            flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 0, 16, 0);
            flowLayoutPanel1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            flowLayoutPanel1.Size = new System.Drawing.Size(363, 40);
            flowLayoutPanel1.TabIndex = 6;
            // 
            // ok
            // 
            this.ok.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ok.Location = new System.Drawing.Point(269, 6);
            this.ok.Name = "ok";
            this.ok.Size = new System.Drawing.Size(75, 31);
            this.ok.TabIndex = 0;
            this.ok.Text = "OK";
            this.ok.UseVisualStyleBackColor = true;
            this.ok.Click += new System.EventHandler(this.ok_Click);
            // 
            // cancel
            // 
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Location = new System.Drawing.Point(188, 6);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(75, 31);
            this.cancel.TabIndex = 1;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            this.cancel.Click += new System.EventHandler(this.cancel_Click);
            // 
            // txtSwitchSystemProxy
            // 
            this.txtSwitchSystemProxy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSwitchSystemProxy.Location = new System.Drawing.Point(183, 3);
            this.txtSwitchSystemProxy.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
            this.txtSwitchSystemProxy.Name = "txtSwitchSystemProxy";
            this.txtSwitchSystemProxy.ReadOnly = true;
            this.txtSwitchSystemProxy.Size = new System.Drawing.Size(164, 25);
            this.txtSwitchSystemProxy.TabIndex = 7;
            this.txtSwitchSystemProxy.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HotkeyDown);
            this.txtSwitchSystemProxy.KeyUp += new System.Windows.Forms.KeyEventHandler(this.HotkeyUp);
            // 
            // txtChangeToPac
            // 
            this.txtChangeToPac.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtChangeToPac.Location = new System.Drawing.Point(183, 38);
            this.txtChangeToPac.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
            this.txtChangeToPac.Name = "txtChangeToPac";
            this.txtChangeToPac.ReadOnly = true;
            this.txtChangeToPac.Size = new System.Drawing.Size(164, 25);
            this.txtChangeToPac.TabIndex = 8;
            this.txtChangeToPac.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HotkeyDown);
            this.txtChangeToPac.KeyUp += new System.Windows.Forms.KeyEventHandler(this.HotkeyUp);
            // 
            // txtChangeToGlobal
            // 
            this.txtChangeToGlobal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtChangeToGlobal.Location = new System.Drawing.Point(183, 73);
            this.txtChangeToGlobal.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
            this.txtChangeToGlobal.Name = "txtChangeToGlobal";
            this.txtChangeToGlobal.ReadOnly = true;
            this.txtChangeToGlobal.Size = new System.Drawing.Size(164, 25);
            this.txtChangeToGlobal.TabIndex = 9;
            this.txtChangeToGlobal.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HotkeyDown);
            this.txtChangeToGlobal.KeyUp += new System.Windows.Forms.KeyEventHandler(this.HotkeyUp);
            // 
            // txtSwitchAllowLan
            // 
            this.txtSwitchAllowLan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSwitchAllowLan.Location = new System.Drawing.Point(183, 108);
            this.txtSwitchAllowLan.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
            this.txtSwitchAllowLan.Name = "txtSwitchAllowLan";
            this.txtSwitchAllowLan.ReadOnly = true;
            this.txtSwitchAllowLan.Size = new System.Drawing.Size(164, 25);
            this.txtSwitchAllowLan.TabIndex = 10;
            this.txtSwitchAllowLan.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HotkeyDown);
            this.txtSwitchAllowLan.KeyUp += new System.Windows.Forms.KeyEventHandler(this.HotkeyUp);
            // 
            // txtShowLogs
            // 
            this.txtShowLogs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtShowLogs.Location = new System.Drawing.Point(183, 143);
            this.txtShowLogs.Margin = new System.Windows.Forms.Padding(3, 3, 16, 3);
            this.txtShowLogs.Name = "txtShowLogs";
            this.txtShowLogs.ReadOnly = true;
            this.txtShowLogs.Size = new System.Drawing.Size(164, 25);
            this.txtShowLogs.TabIndex = 11;
            this.txtShowLogs.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HotkeyDown);
            this.txtShowLogs.KeyUp += new System.Windows.Forms.KeyEventHandler(this.HotkeyUp);
            // 
            // ckbAllowSwitchServer
            // 
            this.ckbAllowSwitchServer.AutoSize = true;
            tableLayoutPanel1.SetColumnSpan(this.ckbAllowSwitchServer, 2);
            this.ckbAllowSwitchServer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ckbAllowSwitchServer.Location = new System.Drawing.Point(3, 178);
            this.ckbAllowSwitchServer.Name = "ckbAllowSwitchServer";
            this.ckbAllowSwitchServer.Size = new System.Drawing.Size(357, 29);
            this.ckbAllowSwitchServer.TabIndex = 12;
            this.ckbAllowSwitchServer.Text = "Allow Change Server Use Ctrl+Alt+Shift+Number";
            this.ckbAllowSwitchServer.UseVisualStyleBackColor = true;
            // 
            // HotkeySettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(363, 250);
            this.Controls.Add(tableLayoutPanel1);
            this.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HotkeySettingsForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HotkeySetting...";
            this.Load += new System.EventHandler(this.HotkeySetting_Load);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblSwitchSystemProxy;
        private System.Windows.Forms.Label lblChangeToPac;
        private System.Windows.Forms.Label lblChangeToGlobal;
        private System.Windows.Forms.Label lblSwitchAllowLan;
        private System.Windows.Forms.Label lblShowLogs;
        private System.Windows.Forms.Button ok;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.TextBox txtSwitchSystemProxy;
        private System.Windows.Forms.TextBox txtChangeToPac;
        private System.Windows.Forms.TextBox txtChangeToGlobal;
        private System.Windows.Forms.TextBox txtSwitchAllowLan;
        private System.Windows.Forms.TextBox txtShowLogs;
        private System.Windows.Forms.CheckBox ckbAllowSwitchServer;
    }
}