namespace Shadowsocks.View
{
    partial class OnlineConfigForm
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
            this.UrlListBox = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.UrlTextBox = new System.Windows.Forms.TextBox();
            this.UpdateButton = new System.Windows.Forms.Button();
            this.AddButton = new System.Windows.Forms.Button();
            this.DeleteButton = new System.Windows.Forms.Button();
            this.OkButton = new System.Windows.Forms.Button();
            this.UpdateAllButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.Controls.Add(this.UrlListBox, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.UrlTextBox, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.UpdateButton, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.AddButton, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.DeleteButton, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.OkButton, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.UpdateAllButton, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.CancelButton, 2, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(488, 465);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // UrlListBox
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.UrlListBox, 3);
            this.UrlListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UrlListBox.FormattingEnabled = true;
            this.UrlListBox.ItemHeight = 15;
            this.UrlListBox.Location = new System.Drawing.Point(3, 3);
            this.UrlListBox.Name = "UrlListBox";
            this.UrlListBox.Size = new System.Drawing.Size(482, 344);
            this.UrlListBox.TabIndex = 0;
            this.UrlListBox.SelectedIndexChanged += new System.EventHandler(this.UrlListBox_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 350);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(156, 31);
            this.label1.TabIndex = 1;
            this.label1.Text = "Online config URL";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // UrlTextBox
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.UrlTextBox, 2);
            this.UrlTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UrlTextBox.Location = new System.Drawing.Point(165, 353);
            this.UrlTextBox.Name = "UrlTextBox";
            this.UrlTextBox.Size = new System.Drawing.Size(320, 25);
            this.UrlTextBox.TabIndex = 2;
            // 
            // UpdateButton
            // 
            this.UpdateButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UpdateButton.Location = new System.Drawing.Point(20, 386);
            this.UpdateButton.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.UpdateButton.MaximumSize = new System.Drawing.Size(0, 32);
            this.UpdateButton.MinimumSize = new System.Drawing.Size(0, 32);
            this.UpdateButton.Name = "UpdateButton";
            this.UpdateButton.Size = new System.Drawing.Size(122, 32);
            this.UpdateButton.TabIndex = 3;
            this.UpdateButton.Text = "Update";
            this.UpdateButton.UseVisualStyleBackColor = true;
            this.UpdateButton.Click += new System.EventHandler(this.UpdateButton_Click);
            // 
            // AddButton
            // 
            this.AddButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AddButton.Location = new System.Drawing.Point(182, 386);
            this.AddButton.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.AddButton.MaximumSize = new System.Drawing.Size(0, 32);
            this.AddButton.MinimumSize = new System.Drawing.Size(0, 32);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(122, 32);
            this.AddButton.TabIndex = 4;
            this.AddButton.Text = "Add";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // DeleteButton
            // 
            this.DeleteButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DeleteButton.Location = new System.Drawing.Point(344, 386);
            this.DeleteButton.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.DeleteButton.MaximumSize = new System.Drawing.Size(0, 32);
            this.DeleteButton.MinimumSize = new System.Drawing.Size(0, 32);
            this.DeleteButton.Name = "DeleteButton";
            this.DeleteButton.Size = new System.Drawing.Size(124, 32);
            this.DeleteButton.TabIndex = 5;
            this.DeleteButton.Text = "Delete";
            this.DeleteButton.UseVisualStyleBackColor = true;
            this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // OkButton
            // 
            this.OkButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OkButton.Location = new System.Drawing.Point(182, 428);
            this.OkButton.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.OkButton.MaximumSize = new System.Drawing.Size(0, 32);
            this.OkButton.MinimumSize = new System.Drawing.Size(0, 32);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(122, 32);
            this.OkButton.TabIndex = 7;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            // 
            // UpdateAllButton
            // 
            this.UpdateAllButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UpdateAllButton.Location = new System.Drawing.Point(20, 428);
            this.UpdateAllButton.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.UpdateAllButton.MaximumSize = new System.Drawing.Size(0, 32);
            this.UpdateAllButton.MinimumSize = new System.Drawing.Size(0, 32);
            this.UpdateAllButton.Name = "UpdateAllButton";
            this.UpdateAllButton.Size = new System.Drawing.Size(122, 32);
            this.UpdateAllButton.TabIndex = 6;
            this.UpdateAllButton.Text = "Update all";
            this.UpdateAllButton.UseVisualStyleBackColor = true;
            this.UpdateAllButton.Click += new System.EventHandler(this.UpdateAllButton_Click);
            // 
            // CancelButton
            // 
            this.CancelButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CancelButton.Location = new System.Drawing.Point(344, 428);
            this.CancelButton.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.CancelButton.MaximumSize = new System.Drawing.Size(0, 32);
            this.CancelButton.MinimumSize = new System.Drawing.Size(0, 32);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(124, 32);
            this.CancelButton.TabIndex = 8;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            // 
            // OnlineConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(488, 465);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "OnlineConfigForm";
            this.Text = "OnlineConfigForm";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ListBox UrlListBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox UrlTextBox;
        private System.Windows.Forms.Button UpdateButton;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.Button DeleteButton;
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.Button UpdateAllButton;
        private System.Windows.Forms.Button CancelButton;
    }
}