namespace Shadowsocks.View
{
    partial class InputPassword
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
            this.textPassword = new System.Windows.Forms.TextBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.label_info = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textPassword
            // 
            this.textPassword.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.textPassword.Location = new System.Drawing.Point(41, 44);
            this.textPassword.Name = "textPassword";
            this.textPassword.Size = new System.Drawing.Size(330, 21);
            this.textPassword.TabIndex = 0;
            this.textPassword.UseSystemPasswordChar = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(131, 76);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(151, 28);
            this.buttonOK.TabIndex = 1;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // label_info
            // 
            this.label_info.AutoSize = true;
            this.label_info.Location = new System.Drawing.Point(24, 26);
            this.label_info.Name = "label_info";
            this.label_info.Size = new System.Drawing.Size(358, 15);
            this.label_info.TabIndex = 2;
            this.label_info.Text = "Parse user-config.json error, maybe require password to decrypt";
            // 
            // InputPassword
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(412, 119);
            this.Controls.Add(this.label_info);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.textPassword);
            this.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.KeyPreview = true;
            this.Name = "InputPassword";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "InputPassword";
            this.TopMost = true;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InputPassword_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textPassword;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Label label_info;
    }
}