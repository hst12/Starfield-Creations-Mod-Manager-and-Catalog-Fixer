namespace hstCMM
{
    partial class frmProfiles
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
            btnOk = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            label1 = new System.Windows.Forms.Label();
            txtProfileDirectory = new System.Windows.Forms.TextBox();
            btnSetProfileDirectory = new System.Windows.Forms.Button();
            flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            btnAdd = new System.Windows.Forms.Button();
            btnDelete = new System.Windows.Forms.Button();
            checkedListBox1 = new System.Windows.Forms.CheckedListBox();
            flowLayoutPanel1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            flowLayoutPanel2.SuspendLayout();
            flowLayoutPanel3.SuspendLayout();
            SuspendLayout();
            // 
            // btnOk
            // 
            btnOk.Location = new System.Drawing.Point(3, 3);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(150, 46);
            btnOk.TabIndex = 0;
            btnOk.Text = "Ok";
            btnOk.UseVisualStyleBackColor = true;
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
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(btnOk);
            flowLayoutPanel1.Controls.Add(btnCancel);
            flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel1.Location = new System.Drawing.Point(3, 592);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new System.Drawing.Size(860, 71);
            flowLayoutPanel1.TabIndex = 2;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 3);
            tableLayoutPanel1.Controls.Add(flowLayoutPanel2, 0, 2);
            tableLayoutPanel1.Controls.Add(flowLayoutPanel3, 0, 1);
            tableLayoutPanel1.Controls.Add(checkedListBox1, 0, 0);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.Size = new System.Drawing.Size(866, 666);
            tableLayoutPanel1.TabIndex = 3;
            // 
            // flowLayoutPanel2
            // 
            flowLayoutPanel2.AutoSize = true;
            flowLayoutPanel2.Controls.Add(label1);
            flowLayoutPanel2.Controls.Add(txtProfileDirectory);
            flowLayoutPanel2.Controls.Add(btnSetProfileDirectory);
            flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel2.Location = new System.Drawing.Point(3, 502);
            flowLayoutPanel2.Name = "flowLayoutPanel2";
            flowLayoutPanel2.Size = new System.Drawing.Size(860, 84);
            flowLayoutPanel2.TabIndex = 3;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(3, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(186, 32);
            label1.TabIndex = 0;
            label1.Text = "Profile Directory";
            // 
            // txtProfileDirectory
            // 
            txtProfileDirectory.Location = new System.Drawing.Point(3, 35);
            txtProfileDirectory.Name = "txtProfileDirectory";
            txtProfileDirectory.Size = new System.Drawing.Size(676, 39);
            txtProfileDirectory.TabIndex = 1;
            // 
            // btnSetProfileDirectory
            // 
            btnSetProfileDirectory.Location = new System.Drawing.Point(685, 35);
            btnSetProfileDirectory.Name = "btnSetProfileDirectory";
            btnSetProfileDirectory.Size = new System.Drawing.Size(150, 46);
            btnSetProfileDirectory.TabIndex = 2;
            btnSetProfileDirectory.Text = "Browse";
            btnSetProfileDirectory.UseVisualStyleBackColor = true;
            btnSetProfileDirectory.Click += btnSetProfileDirectory_Click;
            // 
            // flowLayoutPanel3
            // 
            flowLayoutPanel3.Controls.Add(btnAdd);
            flowLayoutPanel3.Controls.Add(btnDelete);
            flowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel3.Location = new System.Drawing.Point(3, 432);
            flowLayoutPanel3.Name = "flowLayoutPanel3";
            flowLayoutPanel3.Size = new System.Drawing.Size(860, 64);
            flowLayoutPanel3.TabIndex = 5;
            // 
            // btnAdd
            // 
            btnAdd.Location = new System.Drawing.Point(3, 3);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new System.Drawing.Size(150, 46);
            btnAdd.TabIndex = 0;
            btnAdd.Text = "Add";
            btnAdd.UseVisualStyleBackColor = true;
            // 
            // btnDelete
            // 
            btnDelete.Location = new System.Drawing.Point(159, 3);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new System.Drawing.Size(150, 46);
            btnDelete.TabIndex = 1;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            // 
            // checkedListBox1
            // 
            checkedListBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            checkedListBox1.FormattingEnabled = true;
            checkedListBox1.Location = new System.Drawing.Point(3, 3);
            checkedListBox1.Name = "checkedListBox1";
            checkedListBox1.Size = new System.Drawing.Size(860, 423);
            checkedListBox1.TabIndex = 4;
            // 
            // frmProfiles
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(866, 666);
            Controls.Add(tableLayoutPanel1);
            Name = "frmProfiles";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Profile Management";
            flowLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            flowLayoutPanel2.ResumeLayout(false);
            flowLayoutPanel2.PerformLayout();
            flowLayoutPanel3.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtProfileDirectory;
        private System.Windows.Forms.Button btnSetProfileDirectory;
        private System.Windows.Forms.CheckedListBox checkedListBox1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnDelete;
    }
}