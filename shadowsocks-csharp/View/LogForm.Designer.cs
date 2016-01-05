namespace Shadowsocks.View
{
    partial class LogForm
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
            this.components = new System.ComponentModel.Container();
            this.LogMessageTextBox = new System.Windows.Forms.TextBox();
            this.MainMenu = new System.Windows.Forms.MainMenu(this.components);
            this.FileMenuItem = new System.Windows.Forms.MenuItem();
            this.OpenLocationMenuItem = new System.Windows.Forms.MenuItem();
            this.ExitMenuItem = new System.Windows.Forms.MenuItem();
            this.ViewMenuItem = new System.Windows.Forms.MenuItem();
            this.CleanLogsMenuItem = new System.Windows.Forms.MenuItem();
            this.ChangeFontMenuItem = new System.Windows.Forms.MenuItem();
            this.WrapTextMenuItem = new System.Windows.Forms.MenuItem();
            this.TopMostMenuItem = new System.Windows.Forms.MenuItem();
            this.MenuItemSeparater = new System.Windows.Forms.MenuItem();
            this.ShowToolbarMenuItem = new System.Windows.Forms.MenuItem();
            this.TopMostCheckBox = new System.Windows.Forms.CheckBox();
            this.ChangeFontButton = new System.Windows.Forms.Button();
            this.CleanLogsButton = new System.Windows.Forms.Button();
            this.WrapTextCheckBox = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.ToolbarFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.ToolbarFlowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // LogMessageTextBox
            // 
            this.LogMessageTextBox.BackColor = System.Drawing.Color.Black;
            this.LogMessageTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LogMessageTextBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LogMessageTextBox.ForeColor = System.Drawing.Color.White;
            this.LogMessageTextBox.Location = new System.Drawing.Point(3, 40);
            this.LogMessageTextBox.MaxLength = 2147483647;
            this.LogMessageTextBox.Multiline = true;
            this.LogMessageTextBox.Name = "LogMessageTextBox";
            this.LogMessageTextBox.ReadOnly = true;
            this.LogMessageTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.LogMessageTextBox.Size = new System.Drawing.Size(378, 131);
            this.LogMessageTextBox.TabIndex = 0;
            // 
            // MainMenu
            // 
            this.MainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.FileMenuItem,
            this.ViewMenuItem});
            // 
            // FileMenuItem
            // 
            this.FileMenuItem.Index = 0;
            this.FileMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.OpenLocationMenuItem,
            this.ExitMenuItem});
            this.FileMenuItem.Text = "&File";
            // 
            // OpenLocationMenuItem
            // 
            this.OpenLocationMenuItem.Index = 0;
            this.OpenLocationMenuItem.Text = "&Open Location";
            this.OpenLocationMenuItem.Click += new System.EventHandler(this.OpenLocationMenuItem_Click);
            // 
            // ExitMenuItem
            // 
            this.ExitMenuItem.Index = 1;
            this.ExitMenuItem.Text = "E&xit";
            this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
            // 
            // ViewMenuItem
            // 
            this.ViewMenuItem.Index = 1;
            this.ViewMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.CleanLogsMenuItem,
            this.ChangeFontMenuItem,
            this.WrapTextMenuItem,
            this.TopMostMenuItem,
            this.MenuItemSeparater,
            this.ShowToolbarMenuItem});
            this.ViewMenuItem.Text = "&View";
            // 
            // CleanLogsMenuItem
            // 
            this.CleanLogsMenuItem.Index = 0;
            this.CleanLogsMenuItem.Text = "&Clean Logs";
            this.CleanLogsMenuItem.Click += new System.EventHandler(this.CleanLogsMenuItem_Click);
            // 
            // ChangeFontMenuItem
            // 
            this.ChangeFontMenuItem.Index = 1;
            this.ChangeFontMenuItem.Text = "Change &Font";
            this.ChangeFontMenuItem.Click += new System.EventHandler(this.ChangeFontMenuItem_Click);
            // 
            // WrapTextMenuItem
            // 
            this.WrapTextMenuItem.Index = 2;
            this.WrapTextMenuItem.Text = "&Wrap Text";
            this.WrapTextMenuItem.Click += new System.EventHandler(this.WrapTextMenuItem_Click);
            // 
            // TopMostMenuItem
            // 
            this.TopMostMenuItem.Index = 3;
            this.TopMostMenuItem.Text = "&Top Most";
            this.TopMostMenuItem.Click += new System.EventHandler(this.TopMostMenuItem_Click);
            // 
            // MenuItemSeparater
            // 
            this.MenuItemSeparater.Index = 4;
            this.MenuItemSeparater.Text = "-";
            // 
            // ShowToolbarMenuItem
            // 
            this.ShowToolbarMenuItem.Index = 5;
            this.ShowToolbarMenuItem.Text = "&Show Toolbar";
            this.ShowToolbarMenuItem.Click += new System.EventHandler(this.ShowToolbarMenuItem_Click);
            // 
            // TopMostCheckBox
            // 
            this.TopMostCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.TopMostCheckBox.AutoSize = true;
            this.TopMostCheckBox.Location = new System.Drawing.Point(247, 3);
            this.TopMostCheckBox.Name = "TopMostCheckBox";
            this.TopMostCheckBox.Size = new System.Drawing.Size(71, 25);
            this.TopMostCheckBox.TabIndex = 3;
            this.TopMostCheckBox.Text = "&Top Most";
            this.TopMostCheckBox.UseVisualStyleBackColor = true;
            this.TopMostCheckBox.CheckedChanged += new System.EventHandler(this.TopMostCheckBox_CheckedChanged);
            // 
            // ChangeFontButton
            // 
            this.ChangeFontButton.AutoSize = true;
            this.ChangeFontButton.Location = new System.Drawing.Point(84, 3);
            this.ChangeFontButton.Name = "ChangeFontButton";
            this.ChangeFontButton.Size = new System.Drawing.Size(75, 25);
            this.ChangeFontButton.TabIndex = 2;
            this.ChangeFontButton.Text = "&Font";
            this.ChangeFontButton.UseVisualStyleBackColor = true;
            this.ChangeFontButton.Click += new System.EventHandler(this.ChangeFontButton_Click);
            // 
            // CleanLogsButton
            // 
            this.CleanLogsButton.AutoSize = true;
            this.CleanLogsButton.Location = new System.Drawing.Point(3, 3);
            this.CleanLogsButton.Name = "CleanLogsButton";
            this.CleanLogsButton.Size = new System.Drawing.Size(75, 25);
            this.CleanLogsButton.TabIndex = 1;
            this.CleanLogsButton.Text = "&Clean Logs";
            this.CleanLogsButton.UseVisualStyleBackColor = true;
            this.CleanLogsButton.Click += new System.EventHandler(this.CleanLogsButton_Click);
            // 
            // WrapTextCheckBox
            // 
            this.WrapTextCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.WrapTextCheckBox.AutoSize = true;
            this.WrapTextCheckBox.Location = new System.Drawing.Point(165, 3);
            this.WrapTextCheckBox.Name = "WrapTextCheckBox";
            this.WrapTextCheckBox.Size = new System.Drawing.Size(76, 25);
            this.WrapTextCheckBox.TabIndex = 0;
            this.WrapTextCheckBox.Text = "&Wrap Text";
            this.WrapTextCheckBox.UseVisualStyleBackColor = true;
            this.WrapTextCheckBox.CheckedChanged += new System.EventHandler(this.WrapTextCheckBox_CheckedChanged);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.LogMessageTextBox, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.ToolbarFlowLayoutPanel, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(384, 174);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // ToolbarFlowLayoutPanel
            // 
            this.ToolbarFlowLayoutPanel.AutoSize = true;
            this.ToolbarFlowLayoutPanel.Controls.Add(this.CleanLogsButton);
            this.ToolbarFlowLayoutPanel.Controls.Add(this.ChangeFontButton);
            this.ToolbarFlowLayoutPanel.Controls.Add(this.WrapTextCheckBox);
            this.ToolbarFlowLayoutPanel.Controls.Add(this.TopMostCheckBox);
            this.ToolbarFlowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ToolbarFlowLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.ToolbarFlowLayoutPanel.Name = "ToolbarFlowLayoutPanel";
            this.ToolbarFlowLayoutPanel.Size = new System.Drawing.Size(378, 31);
            this.ToolbarFlowLayoutPanel.TabIndex = 2;
            // 
            // LogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 174);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Menu = this.MainMenu;
            this.MinimumSize = new System.Drawing.Size(400, 213);
            this.Name = "LogForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Log Viewer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LogForm_FormClosing);
            this.Load += new System.EventHandler(this.LogForm_Load);
            this.Shown += new System.EventHandler(this.LogForm_Shown);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ToolbarFlowLayoutPanel.ResumeLayout(false);
            this.ToolbarFlowLayoutPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox LogMessageTextBox;
        private System.Windows.Forms.MainMenu MainMenu;
        private System.Windows.Forms.MenuItem FileMenuItem;
        private System.Windows.Forms.MenuItem OpenLocationMenuItem;
        private System.Windows.Forms.MenuItem ExitMenuItem;
        private System.Windows.Forms.CheckBox WrapTextCheckBox;
        private System.Windows.Forms.Button CleanLogsButton;
        private System.Windows.Forms.Button ChangeFontButton;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.CheckBox TopMostCheckBox;
        private System.Windows.Forms.MenuItem ViewMenuItem;
        private System.Windows.Forms.MenuItem CleanLogsMenuItem;
        private System.Windows.Forms.MenuItem ChangeFontMenuItem;
        private System.Windows.Forms.MenuItem WrapTextMenuItem;
        private System.Windows.Forms.MenuItem TopMostMenuItem;
        private System.Windows.Forms.FlowLayoutPanel ToolbarFlowLayoutPanel;
        private System.Windows.Forms.MenuItem MenuItemSeparater;
        private System.Windows.Forms.MenuItem ShowToolbarMenuItem;
    }
}