namespace Shadowsocks.View
{
    partial class QRCodeForm
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
            this.QRCodeWebBrowser = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // QRCodeWebBrowser
            // 
            this.QRCodeWebBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.QRCodeWebBrowser.Location = new System.Drawing.Point(0, 0);
            this.QRCodeWebBrowser.Margin = new System.Windows.Forms.Padding(0);
            this.QRCodeWebBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.QRCodeWebBrowser.Name = "QRCodeWebBrowser";
            this.QRCodeWebBrowser.ScriptErrorsSuppressed = true;
            this.QRCodeWebBrowser.ScrollBarsEnabled = false;
            this.QRCodeWebBrowser.Size = new System.Drawing.Size(200, 200);
            this.QRCodeWebBrowser.TabIndex = 0;
            // 
            // QRCodeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(204, 202);
            this.Controls.Add(this.QRCodeWebBrowser);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "QRCodeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Load += new System.EventHandler(this.QRCodeForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser QRCodeWebBrowser;
    }
}