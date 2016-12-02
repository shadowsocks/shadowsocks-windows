namespace Shadowsocks.View
{
    partial class ResetPassword
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
            this.buttonOK = new System.Windows.Forms.Button();
            this.textOld = new System.Windows.Forms.TextBox();
            this.textPassword = new System.Windows.Forms.TextBox();
            this.textPassword2 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(107, 132);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 3;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // textOld
            // 
            this.textOld.Location = new System.Drawing.Point(28, 13);
            this.textOld.Name = "textOld";
            this.textOld.Size = new System.Drawing.Size(233, 21);
            this.textOld.TabIndex = 0;
            this.textOld.UseSystemPasswordChar = true;
            // 
            // textPassword
            // 
            this.textPassword.Location = new System.Drawing.Point(28, 60);
            this.textPassword.Name = "textPassword";
            this.textPassword.Size = new System.Drawing.Size(233, 21);
            this.textPassword.TabIndex = 1;
            this.textPassword.UseSystemPasswordChar = true;
            // 
            // textPassword2
            // 
            this.textPassword2.Location = new System.Drawing.Point(28, 87);
            this.textPassword2.Name = "textPassword2";
            this.textPassword2.Size = new System.Drawing.Size(233, 21);
            this.textPassword2.TabIndex = 2;
            this.textPassword2.UseSystemPasswordChar = true;
            // 
            // ResetPassword
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 167);
            this.Controls.Add(this.textPassword2);
            this.Controls.Add(this.textPassword);
            this.Controls.Add(this.textOld);
            this.Controls.Add(this.buttonOK);
            this.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.KeyPreview = true;
            this.Name = "ResetPassword";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ResetPassword";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ResetPassword_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.TextBox textOld;
        private System.Windows.Forms.TextBox textPassword;
        private System.Windows.Forms.TextBox textPassword2;
    }
}