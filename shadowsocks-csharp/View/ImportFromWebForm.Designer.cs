namespace Shadowsocks.View
{
    partial class ImportFromWebForm
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
            this.URLTextBox = new System.Windows.Forms.TextBox();
            this.ConfigListBox = new System.Windows.Forms.ListBox();
            this.GetConfigButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.RemoveButton = new System.Windows.Forms.Button();
            this.AddConfigButton = new System.Windows.Forms.Button();
            this.ProviderURLLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.tableLayoutPanel1.Controls.Add(this.URLTextBox, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.ConfigListBox, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.GetConfigButton, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.ProviderURLLabel, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(8, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(383, 259);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // URLTextBox
            // 
            this.URLTextBox.Location = new System.Drawing.Point(32, 3);
            this.URLTextBox.Name = "URLTextBox";
            this.URLTextBox.Size = new System.Drawing.Size(219, 21);
            this.URLTextBox.TabIndex = 1;
            this.URLTextBox.Text = "http://133.130.125.41";
            // 
            // ConfigListBox
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.ConfigListBox, 2);
            this.ConfigListBox.FormattingEnabled = true;
            this.ConfigListBox.ItemHeight = 12;
            this.ConfigListBox.Location = new System.Drawing.Point(32, 32);
            this.ConfigListBox.Name = "ConfigListBox";
            this.ConfigListBox.Size = new System.Drawing.Size(302, 196);
            this.ConfigListBox.TabIndex = 2;
            // 
            // GetConfigButton
            // 
            this.GetConfigButton.Location = new System.Drawing.Point(254, 3);
            this.GetConfigButton.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.GetConfigButton.Name = "GetConfigButton";
            this.GetConfigButton.Size = new System.Drawing.Size(80, 23);
            this.GetConfigButton.TabIndex = 3;
            this.GetConfigButton.Text = "&Get";
            this.GetConfigButton.UseVisualStyleBackColor = true;
            this.GetConfigButton.Click += new System.EventHandler(this.GetConfigButton_Click);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel1.SetColumnSpan(this.tableLayoutPanel2, 2);
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.Controls.Add(this.RemoveButton, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.AddConfigButton, 1, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(29, 231);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(308, 33);
            this.tableLayoutPanel2.TabIndex = 4;
            // 
            // RemoveButton
            // 
            this.RemoveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RemoveButton.AutoEllipsis = true;
            this.RemoveButton.Location = new System.Drawing.Point(195, 3);
            this.RemoveButton.Name = "RemoveButton";
            this.RemoveButton.Size = new System.Drawing.Size(110, 23);
            this.RemoveButton.TabIndex = 0;
            this.RemoveButton.Text = "&Remove";
            this.RemoveButton.UseVisualStyleBackColor = true;
            this.RemoveButton.Click += new System.EventHandler(this.RemoveButton_Click);
            // 
            // AddConfigButton
            // 
            this.AddConfigButton.Location = new System.Drawing.Point(79, 3);
            this.AddConfigButton.Margin = new System.Windows.Forms.Padding(3, 3, 3, 1);
            this.AddConfigButton.Name = "AddConfigButton";
            this.AddConfigButton.Size = new System.Drawing.Size(110, 23);
            this.AddConfigButton.TabIndex = 1;
            this.AddConfigButton.Text = "&Add Config";
            this.AddConfigButton.UseVisualStyleBackColor = true;
            this.AddConfigButton.Click += new System.EventHandler(this.AddConfigButton_Click);
            // 
            // ProviderURLLabel
            // 
            this.ProviderURLLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.ProviderURLLabel.AutoSize = true;
            this.ProviderURLLabel.Location = new System.Drawing.Point(3, 8);
            this.ProviderURLLabel.Name = "ProviderURLLabel";
            this.ProviderURLLabel.Size = new System.Drawing.Size(23, 12);
            this.ProviderURLLabel.TabIndex = 0;
            this.ProviderURLLabel.Text = "URL";
            // 
            // ImportFromWebForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(432, 339);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ImportFromWebForm";
            this.Padding = new System.Windows.Forms.Padding(12, 12, 12, 9);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Import";
            this.Load += new System.EventHandler(this.ImportFromWebForm_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label ProviderURLLabel;
        private System.Windows.Forms.TextBox URLTextBox;
        private System.Windows.Forms.ListBox ConfigListBox;
        private System.Windows.Forms.Button GetConfigButton;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button RemoveButton;
        private System.Windows.Forms.Button AddConfigButton;
    }
}