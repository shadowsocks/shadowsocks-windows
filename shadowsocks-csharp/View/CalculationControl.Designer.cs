namespace Shadowsocks.View
{
    partial class CalculationControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.factorNum = new System.Windows.Forms.NumericUpDown();
            this.multiply = new System.Windows.Forms.Label();
            this.plus = new System.Windows.Forms.Label();
            this.valueLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.factorNum)).BeginInit();
            this.SuspendLayout();
            // 
            // factorNum
            // 
            this.factorNum.DecimalPlaces = 2;
            this.factorNum.Dock = System.Windows.Forms.DockStyle.Right;
            this.factorNum.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.factorNum.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.factorNum.Location = new System.Drawing.Point(236, 0);
            this.factorNum.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147418112});
            this.factorNum.Name = "factorNum";
            this.factorNum.Size = new System.Drawing.Size(86, 34);
            this.factorNum.TabIndex = 6;
            // 
            // multiply
            // 
            this.multiply.AutoSize = true;
            this.multiply.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.multiply.Location = new System.Drawing.Point(202, 2);
            this.multiply.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.multiply.Name = "multiply";
            this.multiply.Size = new System.Drawing.Size(26, 28);
            this.multiply.TabIndex = 2;
            this.multiply.Text = "×";
            // 
            // plus
            // 
            this.plus.AutoSize = true;
            this.plus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.plus.Location = new System.Drawing.Point(5, 2);
            this.plus.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.plus.Name = "plus";
            this.plus.Size = new System.Drawing.Size(26, 28);
            this.plus.TabIndex = 3;
            this.plus.Text = "+";
            // 
            // valueLabel
            // 
            this.valueLabel.AutoSize = true;
            this.valueLabel.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.valueLabel.Location = new System.Drawing.Point(39, 6);
            this.valueLabel.Name = "valueLabel";
            this.valueLabel.Size = new System.Drawing.Size(118, 24);
            this.valueLabel.TabIndex = 7;
            this.valueLabel.Text = "PackageLoss";
            // 
            // CalculationControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.valueLabel);
            this.Controls.Add(this.factorNum);
            this.Controls.Add(this.multiply);
            this.Controls.Add(this.plus);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "CalculationControl";
            this.Size = new System.Drawing.Size(322, 34);
            ((System.ComponentModel.ISupportInitialize)(this.factorNum)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.NumericUpDown factorNum;
        private System.Windows.Forms.Label multiply;
        private System.Windows.Forms.Label plus;
        private System.Windows.Forms.Label valueLabel;
    }
}
