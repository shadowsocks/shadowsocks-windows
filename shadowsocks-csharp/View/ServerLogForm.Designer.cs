namespace Shadowsocks.View
{
    partial class ServerLogForm
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
            this.ServerDataGrid = new System.Windows.Forms.DataGridView();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Server = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Enable = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TotalConnect = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Connecting = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AvgLatency = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AvgSpeed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MaxSpeed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Upload = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Download = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DownloadRaw = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ErrorPercent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ConnectError = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ConnectTimeout = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ConnectEmpty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Continuous = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.ServerDataGrid)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ServerDataGrid
            // 
            this.ServerDataGrid.AllowUserToAddRows = false;
            this.ServerDataGrid.AllowUserToDeleteRows = false;
            this.ServerDataGrid.AllowUserToResizeRows = false;
            this.ServerDataGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ServerDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ServerDataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ID,
            this.Server,
            this.Enable,
            this.TotalConnect,
            this.Connecting,
            this.AvgLatency,
            this.AvgSpeed,
            this.MaxSpeed,
            this.Upload,
            this.Download,
            this.DownloadRaw,
            this.ErrorPercent,
            this.ConnectError,
            this.ConnectTimeout,
            this.ConnectEmpty,
            this.Continuous});
            this.ServerDataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ServerDataGrid.Location = new System.Drawing.Point(0, 0);
            this.ServerDataGrid.Margin = new System.Windows.Forms.Padding(0);
            this.ServerDataGrid.MinimumSize = new System.Drawing.Size(1, 1);
            this.ServerDataGrid.MultiSelect = false;
            this.ServerDataGrid.Name = "ServerDataGrid";
            this.ServerDataGrid.ReadOnly = true;
            this.ServerDataGrid.RowHeadersVisible = false;
            this.ServerDataGrid.RowTemplate.Height = 23;
            this.ServerDataGrid.Size = new System.Drawing.Size(128, 48);
            this.ServerDataGrid.TabIndex = 0;
            this.ServerDataGrid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ServerDataGrid_CellClick);
            this.ServerDataGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ServerDataGrid_CellDoubleClick);
            this.ServerDataGrid.SortCompare += new System.Windows.Forms.DataGridViewSortCompareEventHandler(this.ServerDataGrid_SortCompare);
            this.ServerDataGrid.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ServerDataGrid_MouseUp);
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 250;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.ServerDataGrid, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(128, 48);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // ID
            // 
            this.ID.HeaderText = "ID";
            this.ID.MinimumWidth = 2;
            this.ID.Name = "ID";
            this.ID.ReadOnly = true;
            this.ID.Width = 28;
            // 
            // Server
            // 
            this.Server.HeaderText = "Server";
            this.Server.MinimumWidth = 2;
            this.Server.Name = "Server";
            this.Server.ReadOnly = true;
            // 
            // Enable
            // 
            this.Enable.HeaderText = "Enable";
            this.Enable.MinimumWidth = 2;
            this.Enable.Name = "Enable";
            this.Enable.ReadOnly = true;
            this.Enable.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.Enable.Width = 28;
            // 
            // TotalConnect
            // 
            this.TotalConnect.HeaderText = "Total Connect";
            this.TotalConnect.MinimumWidth = 2;
            this.TotalConnect.Name = "TotalConnect";
            this.TotalConnect.ReadOnly = true;
            this.TotalConnect.Visible = false;
            this.TotalConnect.Width = 48;
            // 
            // Connecting
            // 
            this.Connecting.HeaderText = "Connecting";
            this.Connecting.MinimumWidth = 2;
            this.Connecting.Name = "Connecting";
            this.Connecting.ReadOnly = true;
            this.Connecting.Width = 28;
            // 
            // AvgLatency
            // 
            this.AvgLatency.HeaderText = "Latency";
            this.AvgLatency.MinimumWidth = 2;
            this.AvgLatency.Name = "AvgLatency";
            this.AvgLatency.ReadOnly = true;
            this.AvgLatency.Width = 48;
            // 
            // AvgSpeed
            // 
            this.AvgSpeed.HeaderText = "Avg Speed";
            this.AvgSpeed.MinimumWidth = 2;
            this.AvgSpeed.Name = "AvgSpeed";
            this.AvgSpeed.ReadOnly = true;
            this.AvgSpeed.Width = 72;
            // 
            // MaxSpeed
            // 
            this.MaxSpeed.HeaderText = "Max Speed";
            this.MaxSpeed.MinimumWidth = 2;
            this.MaxSpeed.Name = "MaxSpeed";
            this.MaxSpeed.ReadOnly = true;
            this.MaxSpeed.Width = 72;
            // 
            // Upload
            // 
            this.Upload.HeaderText = "Upload";
            this.Upload.MinimumWidth = 2;
            this.Upload.Name = "Upload";
            this.Upload.ReadOnly = true;
            this.Upload.Width = 72;
            // 
            // Download
            // 
            this.Download.HeaderText = "Download";
            this.Download.MinimumWidth = 2;
            this.Download.Name = "Download";
            this.Download.ReadOnly = true;
            this.Download.Width = 72;
            // 
            // DownloadRaw
            // 
            this.DownloadRaw.HeaderText = "DownloadRaw";
            this.DownloadRaw.MinimumWidth = 2;
            this.DownloadRaw.Name = "DownloadRaw";
            this.DownloadRaw.ReadOnly = true;
            this.DownloadRaw.Width = 72;
            // 
            // ErrorPercent
            // 
            this.ErrorPercent.HeaderText = "Error Percent";
            this.ErrorPercent.MinimumWidth = 2;
            this.ErrorPercent.Name = "ErrorPercent";
            this.ErrorPercent.ReadOnly = true;
            this.ErrorPercent.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.ErrorPercent.Width = 48;
            // 
            // ConnectError
            // 
            this.ConnectError.HeaderText = "Error";
            this.ConnectError.MinimumWidth = 2;
            this.ConnectError.Name = "ConnectError";
            this.ConnectError.ReadOnly = true;
            this.ConnectError.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.ConnectError.Width = 28;
            // 
            // ConnectTimeout
            // 
            this.ConnectTimeout.HeaderText = "Timeout";
            this.ConnectTimeout.MinimumWidth = 2;
            this.ConnectTimeout.Name = "ConnectTimeout";
            this.ConnectTimeout.ReadOnly = true;
            this.ConnectTimeout.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.ConnectTimeout.Width = 28;
            // 
            // ConnectEmpty
            // 
            this.ConnectEmpty.HeaderText = "Empty Response";
            this.ConnectEmpty.MinimumWidth = 2;
            this.ConnectEmpty.Name = "ConnectEmpty";
            this.ConnectEmpty.ReadOnly = true;
            this.ConnectEmpty.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.ConnectEmpty.Width = 28;
            // 
            // Continuous
            // 
            this.Continuous.HeaderText = "Continuous";
            this.Continuous.Name = "Continuous";
            this.Continuous.ReadOnly = true;
            this.Continuous.Visible = false;
            this.Continuous.Width = 28;
            // 
            // ServerLogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(128, 48);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("新宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "ServerLogForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ServerLog";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ServerLogForm_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.ServerDataGrid)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView ServerDataGrid;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.DataGridViewTextBoxColumn ID;
        private System.Windows.Forms.DataGridViewTextBoxColumn Server;
        private System.Windows.Forms.DataGridViewTextBoxColumn Enable;
        private System.Windows.Forms.DataGridViewTextBoxColumn TotalConnect;
        private System.Windows.Forms.DataGridViewTextBoxColumn Connecting;
        private System.Windows.Forms.DataGridViewTextBoxColumn AvgLatency;
        private System.Windows.Forms.DataGridViewTextBoxColumn AvgSpeed;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxSpeed;
        private System.Windows.Forms.DataGridViewTextBoxColumn Upload;
        private System.Windows.Forms.DataGridViewTextBoxColumn Download;
        private System.Windows.Forms.DataGridViewTextBoxColumn DownloadRaw;
        private System.Windows.Forms.DataGridViewTextBoxColumn ErrorPercent;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConnectError;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConnectTimeout;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConnectEmpty;
        private System.Windows.Forms.DataGridViewTextBoxColumn Continuous;
    }
}