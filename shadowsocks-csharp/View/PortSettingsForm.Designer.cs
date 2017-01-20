namespace Shadowsocks.View
{
    partial class PortSettingsForm
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
            this.listPorts = new System.Windows.Forms.ListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.labelType = new System.Windows.Forms.Label();
            this.labelID = new System.Windows.Forms.Label();
            this.labelAddr = new System.Windows.Forms.Label();
            this.labelPort = new System.Windows.Forms.Label();
            this.checkEnable = new System.Windows.Forms.CheckBox();
            this.textAddr = new System.Windows.Forms.TextBox();
            this.NumTargetPort = new System.Windows.Forms.NumericUpDown();
            this.comboBoxType = new System.Windows.Forms.ComboBox();
            this.comboServers = new System.Windows.Forms.ComboBox();
            this.labelLocal = new System.Windows.Forms.Label();
            this.NumLocalPort = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.textRemarks = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.OKButton = new System.Windows.Forms.Button();
            this.MyCancelButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.Add = new System.Windows.Forms.Button();
            this.Del = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NumTargetPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NumLocalPort)).BeginInit();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.listPorts, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.groupBox1, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel4, 0, 1);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(13, 13);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(684, 325);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // listPorts
            // 
            this.listPorts.FormattingEnabled = true;
            this.listPorts.ItemHeight = 12;
            this.listPorts.Location = new System.Drawing.Point(3, 3);
            this.listPorts.Name = "listPorts";
            this.listPorts.Size = new System.Drawing.Size(144, 244);
            this.listPorts.TabIndex = 0;
            this.listPorts.SelectedIndexChanged += new System.EventHandler(this.listPorts_SelectedIndexChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanel2);
            this.groupBox1.Location = new System.Drawing.Point(153, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(531, 255);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Map Setting";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.Controls.Add(this.labelType, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.labelID, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.labelAddr, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.labelPort, 0, 5);
            this.tableLayoutPanel2.Controls.Add(this.checkEnable, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.textAddr, 1, 4);
            this.tableLayoutPanel2.Controls.Add(this.NumTargetPort, 1, 5);
            this.tableLayoutPanel2.Controls.Add(this.comboBoxType, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.comboServers, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.labelLocal, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.NumLocalPort, 1, 3);
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 6);
            this.tableLayoutPanel2.Controls.Add(this.textRemarks, 1, 6);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(7, 21);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 8;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(515, 223);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // labelType
            // 
            this.labelType.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.labelType.AutoSize = true;
            this.labelType.Location = new System.Drawing.Point(45, 29);
            this.labelType.Name = "labelType";
            this.labelType.Size = new System.Drawing.Size(29, 12);
            this.labelType.TabIndex = 0;
            this.labelType.Text = "Type";
            // 
            // labelID
            // 
            this.labelID.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.labelID.AutoSize = true;
            this.labelID.Location = new System.Drawing.Point(15, 55);
            this.labelID.Name = "labelID";
            this.labelID.Size = new System.Drawing.Size(59, 12);
            this.labelID.TabIndex = 0;
            this.labelID.Text = "Server ID";
            // 
            // labelAddr
            // 
            this.labelAddr.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.labelAddr.AutoSize = true;
            this.labelAddr.Location = new System.Drawing.Point(3, 108);
            this.labelAddr.Name = "labelAddr";
            this.labelAddr.Size = new System.Drawing.Size(71, 12);
            this.labelAddr.TabIndex = 0;
            this.labelAddr.Text = "Target Addr";
            // 
            // labelPort
            // 
            this.labelPort.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.labelPort.AutoSize = true;
            this.labelPort.Location = new System.Drawing.Point(3, 135);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(71, 12);
            this.labelPort.TabIndex = 0;
            this.labelPort.Text = "Target Port";
            // 
            // checkEnable
            // 
            this.checkEnable.AutoSize = true;
            this.checkEnable.Location = new System.Drawing.Point(80, 3);
            this.checkEnable.Name = "checkEnable";
            this.checkEnable.Size = new System.Drawing.Size(60, 16);
            this.checkEnable.TabIndex = 3;
            this.checkEnable.Text = "Enable";
            this.checkEnable.UseVisualStyleBackColor = true;
            // 
            // textAddr
            // 
            this.textAddr.Location = new System.Drawing.Point(80, 104);
            this.textAddr.Name = "textAddr";
            this.textAddr.Size = new System.Drawing.Size(403, 21);
            this.textAddr.TabIndex = 7;
            // 
            // NumTargetPort
            // 
            this.NumTargetPort.Location = new System.Drawing.Point(80, 131);
            this.NumTargetPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.NumTargetPort.Name = "NumTargetPort";
            this.NumTargetPort.Size = new System.Drawing.Size(403, 21);
            this.NumTargetPort.TabIndex = 8;
            // 
            // comboBoxType
            // 
            this.comboBoxType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxType.FormattingEnabled = true;
            this.comboBoxType.Items.AddRange(new object[] {
            "Port Forward",
            "Force Proxy",
            "Proxy With Rule"});
            this.comboBoxType.Location = new System.Drawing.Point(80, 25);
            this.comboBoxType.Name = "comboBoxType";
            this.comboBoxType.Size = new System.Drawing.Size(403, 20);
            this.comboBoxType.TabIndex = 4;
            this.comboBoxType.SelectedIndexChanged += new System.EventHandler(this.comboBoxType_SelectedIndexChanged);
            // 
            // comboServers
            // 
            this.comboServers.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboServers.FormattingEnabled = true;
            this.comboServers.Location = new System.Drawing.Point(80, 51);
            this.comboServers.Name = "comboServers";
            this.comboServers.Size = new System.Drawing.Size(403, 20);
            this.comboServers.TabIndex = 5;
            // 
            // labelLocal
            // 
            this.labelLocal.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.labelLocal.AutoSize = true;
            this.labelLocal.Location = new System.Drawing.Point(9, 81);
            this.labelLocal.Name = "labelLocal";
            this.labelLocal.Size = new System.Drawing.Size(65, 12);
            this.labelLocal.TabIndex = 0;
            this.labelLocal.Text = "Local Port";
            // 
            // NumLocalPort
            // 
            this.NumLocalPort.Location = new System.Drawing.Point(80, 77);
            this.NumLocalPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.NumLocalPort.Name = "NumLocalPort";
            this.NumLocalPort.Size = new System.Drawing.Size(403, 21);
            this.NumLocalPort.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 162);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "Remarks";
            // 
            // textRemarks
            // 
            this.textRemarks.Location = new System.Drawing.Point(80, 158);
            this.textRemarks.Name = "textRemarks";
            this.textRemarks.Size = new System.Drawing.Size(403, 21);
            this.textRemarks.TabIndex = 9;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Controls.Add(this.OKButton, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.MyCancelButton, 1, 0);
            this.tableLayoutPanel3.Location = new System.Drawing.Point(153, 264);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(490, 54);
            this.tableLayoutPanel3.TabIndex = 3;
            // 
            // OKButton
            // 
            this.OKButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.OKButton.Location = new System.Drawing.Point(46, 3);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(152, 35);
            this.OKButton.TabIndex = 10;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // MyCancelButton
            // 
            this.MyCancelButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.MyCancelButton.Location = new System.Drawing.Point(291, 3);
            this.MyCancelButton.Name = "MyCancelButton";
            this.MyCancelButton.Size = new System.Drawing.Size(152, 35);
            this.MyCancelButton.TabIndex = 11;
            this.MyCancelButton.Text = "Cancel";
            this.MyCancelButton.UseVisualStyleBackColor = true;
            this.MyCancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 1;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Controls.Add(this.Add, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.Del, 0, 1);
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 264);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 2;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(144, 54);
            this.tableLayoutPanel4.TabIndex = 1;
            // 
            // Add
            // 
            this.Add.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.Add.Location = new System.Drawing.Point(22, 3);
            this.Add.Name = "Add";
            this.Add.Size = new System.Drawing.Size(100, 21);
            this.Add.TabIndex = 1;
            this.Add.Text = "&Add";
            this.Add.UseVisualStyleBackColor = true;
            this.Add.Click += new System.EventHandler(this.Add_Click);
            // 
            // Del
            // 
            this.Del.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.Del.Location = new System.Drawing.Point(22, 30);
            this.Del.Name = "Del";
            this.Del.Size = new System.Drawing.Size(100, 21);
            this.Del.TabIndex = 2;
            this.Del.Text = "&Delete";
            this.Del.UseVisualStyleBackColor = true;
            this.Del.Click += new System.EventHandler(this.Del_Click);
            // 
            // PortSettingsForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(727, 489);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "PortSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Port Settings";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.PortMapForm_FormClosed);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NumTargetPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NumLocalPort)).EndInit();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ListBox listPorts;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.Button MyCancelButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label labelType;
        private System.Windows.Forms.Label labelID;
        private System.Windows.Forms.Label labelAddr;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.CheckBox checkEnable;
        private System.Windows.Forms.TextBox textAddr;
        private System.Windows.Forms.NumericUpDown NumTargetPort;
        private System.Windows.Forms.ComboBox comboBoxType;
        private System.Windows.Forms.ComboBox comboServers;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label labelLocal;
        private System.Windows.Forms.NumericUpDown NumLocalPort;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.Button Add;
        private System.Windows.Forms.Button Del;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textRemarks;
    }
}