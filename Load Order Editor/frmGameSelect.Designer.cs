namespace hstCMM.Load_Order_Editor
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
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            flowLayoutPanel1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // btnSelect
            // 
            btnSelect.Location = new System.Drawing.Point(3, 158);
            btnSelect.Name = "btnSelect";
            btnSelect.Size = new System.Drawing.Size(150, 46);
            btnSelect.TabIndex = 1;
            btnSelect.Text = "Select";
            btnSelect.UseVisualStyleBackColor = true;
            btnSelect.Click += btnSelect_Click;
            // 
            // btnCancel
            // 
            btnCancel.Dock = System.Windows.Forms.DockStyle.Right;
            btnCancel.Location = new System.Drawing.Point(328, 158);
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
            radStarfield.Location = new System.Drawing.Point(3, 3);
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
            radFallout5.Location = new System.Drawing.Point(3, 45);
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
            radES6.Location = new System.Drawing.Point(3, 87);
            radES6.Name = "radES6";
            radES6.Size = new System.Drawing.Size(193, 36);
            radES6.TabIndex = 5;
            radES6.TabStop = true;
            radES6.Text = "Elder Scrolls 6";
            radES6.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(radStarfield);
            flowLayoutPanel1.Controls.Add(radFallout5);
            flowLayoutPanel1.Controls.Add(radES6);
            flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new System.Drawing.Size(314, 149);
            flowLayoutPanel1.TabIndex = 6;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66.6666641F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.Controls.Add(btnCancel, 1, 1);
            tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 0);
            tableLayoutPanel1.Controls.Add(btnSelect, 0, 1);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.Size = new System.Drawing.Size(481, 207);
            tableLayoutPanel1.TabIndex = 7;
            // 
            // frmGameSelect
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(481, 207);
            Controls.Add(tableLayoutPanel1);
            MaximizeBox = false;
            Name = "frmGameSelect";
            Text = "Select Game";
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.RadioButton radStarfield;
        private System.Windows.Forms.RadioButton radFallout5;
        private System.Windows.Forms.RadioButton radES6;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}