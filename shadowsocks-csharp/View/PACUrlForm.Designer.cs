namespace Shadowsocks.View
{
    partial class PACUrlForm
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
            this.PACUrlTextBox = new System.Windows.Forms.TextBox();
            this.PACUrlLabel = new System.Windows.Forms.Label();
            this.OkButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // PACUrlTextBox
            // 
            this.PACUrlTextBox.Location = new System.Drawing.Point(61, 12);
            this.PACUrlTextBox.MaxLength = 256;
            this.PACUrlTextBox.Name = "PACUrlTextBox";
            this.PACUrlTextBox.Size = new System.Drawing.Size(245, 20);
            this.PACUrlTextBox.TabIndex = 4;
            this.PACUrlTextBox.WordWrap = false;
            // 
            // PACUrlLabel
            // 
            this.PACUrlLabel.AutoSize = true;
            this.PACUrlLabel.Location = new System.Drawing.Point(6, 15);
            this.PACUrlLabel.Name = "PACUrlLabel";
            this.PACUrlLabel.Size = new System.Drawing.Size(44, 13);
            this.PACUrlLabel.TabIndex = 3;
            this.PACUrlLabel.Text = "PAC Url";
            // 
            // OkButton
            // 
            this.OkButton.Location = new System.Drawing.Point(150, 50);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(75, 23);
            this.OkButton.TabIndex = 5;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // CancelButton
            // 
            this.CancelButton.Location = new System.Drawing.Point(231, 50);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(75, 23);
            this.CancelButton.TabIndex = 6;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // PACUrlForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(327, 88);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.PACUrlTextBox);
            this.Controls.Add(this.PACUrlLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PACUrlForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Update Online PAC URL";
            this.Load += new System.EventHandler(this.PACUrlForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox PACUrlTextBox;
        private System.Windows.Forms.Label PACUrlLabel;
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.Button CancelButton;
    }
}