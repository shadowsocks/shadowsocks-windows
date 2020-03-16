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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.LogMessageTextBox = new System.Windows.Forms.TextBox();
            this.MainMenu = new System.Windows.Forms.MenuStrip();
            this.FileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenLocationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ClearLogsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ChangeFontToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.WrapTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TopMostToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItemSeparater = new System.Windows.Forms.ToolStripMenuItem();
            this.ShowToolbarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TopMostCheckBox = new System.Windows.Forms.CheckBox();
            this.ChangeFontButton = new System.Windows.Forms.Button();
            this.ClearLogsButton = new System.Windows.Forms.Button();
            this.WrapTextCheckBox = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.ToolbarFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.trafficChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tableLayoutPanel1.SuspendLayout();
            this.ToolbarFlowLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trafficChart)).BeginInit();
            this.SuspendLayout();
            // 
            // LogMessageTextBox
            // 
            this.LogMessageTextBox.BackColor = System.Drawing.Color.Black;
            this.LogMessageTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LogMessageTextBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LogMessageTextBox.ForeColor = System.Drawing.Color.White;
            this.LogMessageTextBox.Location = new System.Drawing.Point(0, 0);
            this.LogMessageTextBox.MaxLength = 2147483647;
            this.LogMessageTextBox.Multiline = true;
            this.LogMessageTextBox.Name = "LogMessageTextBox";
            this.LogMessageTextBox.ReadOnly = true;
            this.LogMessageTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.LogMessageTextBox.Size = new System.Drawing.Size(378, 74);
            this.LogMessageTextBox.TabIndex = 0;
            // 
            // MainMenu
            // 
            this.MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripMenuItem[] {
            this.FileToolStripMenuItem,
            this.ViewToolStripMenuItem});
            // 
            // FileToolStripMenuItem
            // 
            this.FileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripMenuItem[] {
            this.OpenLocationToolStripMenuItem,
            this.ExitToolStripMenuItem});
            this.FileToolStripMenuItem.Text = "&File";
            // 
            // OpenLocationToolStripMenuItem
            // 
            this.OpenLocationToolStripMenuItem.Text = "&Open Location";
            this.OpenLocationToolStripMenuItem.Click += new System.EventHandler(this.OpenLocationToolStripMenuItem_Click);
            // 
            // ExitToolStripMenuItem
            // 
            this.ExitToolStripMenuItem.Text = "E&xit";
            this.ExitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // ViewToolStripMenuItem
            // 
            this.ViewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripMenuItem[] {
            this.ClearLogsToolStripMenuItem,
            this.ChangeFontToolStripMenuItem,
            this.WrapTextToolStripMenuItem,
            this.TopMostToolStripMenuItem,
            this.ToolStripMenuItemSeparater,
            this.ShowToolbarToolStripMenuItem});
            this.ViewToolStripMenuItem.Text = "&View";
            // 
            // ClearLogsToolStripMenuItem
            // 
            this.ClearLogsToolStripMenuItem.Text = "&Clear Logs";
            this.ClearLogsToolStripMenuItem.Click += new System.EventHandler(this.ClearLogsToolStripMenuItem_Click);
            // 
            // ChangeFontToolStripMenuItem
            // 
            this.ChangeFontToolStripMenuItem.Text = "Change &Font";
            this.ChangeFontToolStripMenuItem.Click += new System.EventHandler(this.ChangeFontToolStripMenuItem_Click);
            // 
            // WrapTextToolStripMenuItem
            // 
            this.WrapTextToolStripMenuItem.Text = "&Wrap Text";
            this.WrapTextToolStripMenuItem.Click += new System.EventHandler(this.WrapTextToolStripMenuItem_Click);
            // 
            // TopMostToolStripMenuItem
            // 
            this.TopMostToolStripMenuItem.Text = "&Top Most";
            this.TopMostToolStripMenuItem.Click += new System.EventHandler(this.TopMostToolStripMenuItem_Click);
            // 
            // ToolStripMenuItemSeparater
            // 
            this.ToolStripMenuItemSeparater.Text = "-";
            // 
            // ShowToolbarToolStripMenuItem
            // 
            this.ShowToolbarToolStripMenuItem.Text = "&Show Toolbar";
            this.ShowToolbarToolStripMenuItem.Click += new System.EventHandler(this.ShowToolbarToolStripMenuItem_Click);
            // 
            // TopMostCheckBox
            // 
            this.TopMostCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.TopMostCheckBox.AutoSize = true;
            this.TopMostCheckBox.Location = new System.Drawing.Point(249, 3);
            this.TopMostCheckBox.Name = "TopMostCheckBox";
            this.TopMostCheckBox.Size = new System.Drawing.Size(72, 23);
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
            this.ChangeFontButton.Size = new System.Drawing.Size(75, 23);
            this.ChangeFontButton.TabIndex = 2;
            this.ChangeFontButton.Text = "&Font";
            this.ChangeFontButton.UseVisualStyleBackColor = true;
            this.ChangeFontButton.Click += new System.EventHandler(this.ChangeFontButton_Click);
            // 
            // ClearLogsButton
            // 
            this.ClearLogsButton.AutoSize = true;
            this.ClearLogsButton.Location = new System.Drawing.Point(3, 3);
            this.ClearLogsButton.Name = "ClearLogsButton";
            this.ClearLogsButton.Size = new System.Drawing.Size(75, 23);
            this.ClearLogsButton.TabIndex = 1;
            this.ClearLogsButton.Text = "&Clear Logs";
            this.ClearLogsButton.UseVisualStyleBackColor = true;
            this.ClearLogsButton.Click += new System.EventHandler(this.ClearLogsButton_Click);
            // 
            // WrapTextCheckBox
            // 
            this.WrapTextCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.WrapTextCheckBox.AutoSize = true;
            this.WrapTextCheckBox.Location = new System.Drawing.Point(165, 3);
            this.WrapTextCheckBox.Name = "WrapTextCheckBox";
            this.WrapTextCheckBox.Size = new System.Drawing.Size(78, 23);
            this.WrapTextCheckBox.TabIndex = 0;
            this.WrapTextCheckBox.Text = "&Wrap Text";
            this.WrapTextCheckBox.UseVisualStyleBackColor = true;
            this.WrapTextCheckBox.CheckedChanged += new System.EventHandler(this.WrapTextCheckBox_CheckedChanged);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.ToolbarFlowLayoutPanel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.splitContainer1, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(384, 161);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // ToolbarFlowLayoutPanel
            // 
            this.ToolbarFlowLayoutPanel.AutoSize = true;
            this.ToolbarFlowLayoutPanel.Controls.Add(this.ClearLogsButton);
            this.ToolbarFlowLayoutPanel.Controls.Add(this.ChangeFontButton);
            this.ToolbarFlowLayoutPanel.Controls.Add(this.WrapTextCheckBox);
            this.ToolbarFlowLayoutPanel.Controls.Add(this.TopMostCheckBox);
            this.ToolbarFlowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ToolbarFlowLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.ToolbarFlowLayoutPanel.Name = "ToolbarFlowLayoutPanel";
            this.ToolbarFlowLayoutPanel.Size = new System.Drawing.Size(378, 29);
            this.ToolbarFlowLayoutPanel.TabIndex = 2;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 38);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.LogMessageTextBox);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.trafficChart);
            this.splitContainer1.Size = new System.Drawing.Size(378, 120);
            this.splitContainer1.SplitterDistance = 74;
            this.splitContainer1.TabIndex = 3;
            // 
            // trafficChart
            // 
            chartArea1.AxisX.LabelStyle.Enabled = false;
            chartArea1.AxisX.MajorGrid.Interval = 5D;
            chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea1.AxisX.MajorTickMark.Enabled = false;
            chartArea1.AxisX.Maximum = 61D;
            chartArea1.AxisX.Minimum = 1D;
            chartArea1.AxisY.IntervalAutoMode = System.Windows.Forms.DataVisualization.Charting.IntervalAutoMode.VariableCount;
            chartArea1.AxisY.LabelAutoFitMaxFontSize = 8;
            chartArea1.AxisY.LabelStyle.Interval = 0D;
            chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea1.AxisY.MajorTickMark.Enabled = false;
            chartArea1.AxisY2.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea1.AxisY2.Minimum = 0D;
            chartArea1.Name = "ChartArea1";
            this.trafficChart.ChartAreas.Add(chartArea1);
            this.trafficChart.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.MaximumAutoSize = 25F;
            legend1.Name = "Legend1";
            this.trafficChart.Legends.Add(legend1);
            this.trafficChart.Location = new System.Drawing.Point(0, 0);
            this.trafficChart.Name = "trafficChart";
            this.trafficChart.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.None;
            series1.BorderWidth = 2;
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            series1.Color = System.Drawing.Color.FromArgb(255, 128, 0);
            series1.IsXValueIndexed = true;
            series1.Legend = "Legend1";
            series1.Name = "Inbound";
            series2.BorderWidth = 2;
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            series2.Color = System.Drawing.Color.FromArgb(128, 128, 255);
            series2.IsXValueIndexed = true;
            series2.Legend = "Legend1";
            series2.Name = "Outbound";
            this.trafficChart.Series.Add(series1);
            this.trafficChart.Series.Add(series2);
            this.trafficChart.Size = new System.Drawing.Size(378, 42);
            this.trafficChart.TabIndex = 0;
            this.trafficChart.Text = "chart1";
            // 
            // LogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(384, 161);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MainMenuStrip = this.MainMenu;
            this.MinimumSize = new System.Drawing.Size(400, 200);
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
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.trafficChart)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox LogMessageTextBox;
        private System.Windows.Forms.MenuStrip   MainMenu;
        private System.Windows.Forms.ToolStripMenuItem FileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OpenLocationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
        private System.Windows.Forms.CheckBox WrapTextCheckBox;
        private System.Windows.Forms.Button ClearLogsButton;
        private System.Windows.Forms.Button ChangeFontButton;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.CheckBox TopMostCheckBox;
        private System.Windows.Forms.ToolStripMenuItem ViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ClearLogsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ChangeFontToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem WrapTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem TopMostToolStripMenuItem;
        private System.Windows.Forms.FlowLayoutPanel ToolbarFlowLayoutPanel;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemSeparater;
        private System.Windows.Forms.ToolStripMenuItem ShowToolbarToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataVisualization.Charting.Chart trafficChart;
    }
}