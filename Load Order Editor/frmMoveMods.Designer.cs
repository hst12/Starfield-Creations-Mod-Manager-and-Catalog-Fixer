namespace hstCMM.Load_Order_Editor
{
    partial class frmMoveMods
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
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            btnOk = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            radCreations = new System.Windows.Forms.RadioButton();
            radOther = new System.Windows.Forms.RadioButton();
            radBoth = new System.Windows.Forms.RadioButton();
            tableLayoutPanel1.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            flowLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 1);
            tableLayoutPanel1.Controls.Add(flowLayoutPanel2, 0, 0);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.Size = new System.Drawing.Size(339, 230);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.Controls.Add(btnOk);
            flowLayoutPanel1.Controls.Add(btnCancel);
            flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel1.Location = new System.Drawing.Point(3, 175);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new System.Drawing.Size(333, 52);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // btnOk
            // 
            btnOk.Location = new System.Drawing.Point(3, 3);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(150, 46);
            btnOk.TabIndex = 0;
            btnOk.Text = "Ok";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new System.Drawing.Point(159, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(150, 46);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // flowLayoutPanel2
            // 
            flowLayoutPanel2.Controls.Add(radCreations);
            flowLayoutPanel2.Controls.Add(radOther);
            flowLayoutPanel2.Controls.Add(radBoth);
            flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel2.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            flowLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            flowLayoutPanel2.Name = "flowLayoutPanel2";
            flowLayoutPanel2.Size = new System.Drawing.Size(333, 166);
            flowLayoutPanel2.TabIndex = 1;
            // 
            // radCreations
            // 
            radCreations.AutoSize = true;
            radCreations.Location = new System.Drawing.Point(3, 3);
            radCreations.Name = "radCreations";
            radCreations.Size = new System.Drawing.Size(145, 36);
            radCreations.TabIndex = 0;
            radCreations.TabStop = true;
            radCreations.Text = "Creations";
            radCreations.UseVisualStyleBackColor = true;
            // 
            // radOther
            // 
            radOther.AutoSize = true;
            radOther.Location = new System.Drawing.Point(3, 45);
            radOther.Name = "radOther";
            radOther.Size = new System.Drawing.Size(106, 36);
            radOther.TabIndex = 1;
            radOther.TabStop = true;
            radOther.Text = "Other";
            radOther.UseVisualStyleBackColor = true;
            // 
            // radBoth
            // 
            radBoth.AutoSize = true;
            radBoth.Location = new System.Drawing.Point(3, 87);
            radBoth.Name = "radBoth";
            radBoth.Size = new System.Drawing.Size(95, 36);
            radBoth.TabIndex = 2;
            radBoth.TabStop = true;
            radBoth.Text = "Both";
            radBoth.UseVisualStyleBackColor = true;
            // 
            // frmMoveMods
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(339, 230);
            Controls.Add(tableLayoutPanel1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmMoveMods";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Move Mods";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel2.ResumeLayout(false);
            flowLayoutPanel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.RadioButton radCreations;
        private System.Windows.Forms.RadioButton radOther;
        private System.Windows.Forms.RadioButton radBoth;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}