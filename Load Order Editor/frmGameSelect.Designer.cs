namespace Starfield_Tools.Load_Order_Editor
{
    partial class frmGameSelect
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
            btnSelect = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            radStarfield = new System.Windows.Forms.RadioButton();
            radFallout5 = new System.Windows.Forms.RadioButton();
            radES6 = new System.Windows.Forms.RadioButton();
            SuspendLayout();
            // 
            // btnSelect
            // 
            btnSelect.Location = new System.Drawing.Point(12, 392);
            btnSelect.Name = "btnSelect";
            btnSelect.Size = new System.Drawing.Size(150, 46);
            btnSelect.TabIndex = 1;
            btnSelect.Text = "Select";
            btnSelect.UseVisualStyleBackColor = true;
            btnSelect.Click += btnSelect_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new System.Drawing.Point(638, 392);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(150, 46);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // radStarfield
            // 
            radStarfield.AutoSize = true;
            radStarfield.Location = new System.Drawing.Point(12, 17);
            radStarfield.Name = "radStarfield";
            radStarfield.Size = new System.Drawing.Size(132, 36);
            radStarfield.TabIndex = 3;
            radStarfield.TabStop = true;
            radStarfield.Text = "Starfield";
            radStarfield.UseVisualStyleBackColor = true;
            // 
            // radFallout5
            // 
            radFallout5.AutoSize = true;
            radFallout5.Location = new System.Drawing.Point(12, 59);
            radFallout5.Name = "radFallout5";
            radFallout5.Size = new System.Drawing.Size(136, 36);
            radFallout5.TabIndex = 4;
            radFallout5.TabStop = true;
            radFallout5.Text = "Fallout 5";
            radFallout5.UseVisualStyleBackColor = true;
            // 
            // radES6
            // 
            radES6.AutoSize = true;
            radES6.Location = new System.Drawing.Point(12, 101);
            radES6.Name = "radES6";
            radES6.Size = new System.Drawing.Size(193, 36);
            radES6.TabIndex = 5;
            radES6.TabStop = true;
            radES6.Text = "Elder Scrolls 6";
            radES6.UseVisualStyleBackColor = true;
            // 
            // frmGameSelect
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 450);
            Controls.Add(radES6);
            Controls.Add(radFallout5);
            Controls.Add(radStarfield);
            Controls.Add(btnCancel);
            Controls.Add(btnSelect);
            Name = "frmGameSelect";
            Text = "Select Game";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.RadioButton radStarfield;
        private System.Windows.Forms.RadioButton radFallout5;
        private System.Windows.Forms.RadioButton radES6;
    }
}