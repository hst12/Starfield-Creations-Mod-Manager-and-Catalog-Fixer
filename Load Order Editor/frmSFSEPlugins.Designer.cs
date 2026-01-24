namespace hstCMM.Load_Order_Editor
{
    partial class frmSFSEPlugins
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
            chkSFSEPlugins = new System.Windows.Forms.CheckedListBox();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            btnSelectAll = new System.Windows.Forms.Button();
            btnSelectNone = new System.Windows.Forms.Button();
            btnOpenFolder = new System.Windows.Forms.Button();
            btnOk = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            tableLayoutPanel1.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // chkSFSEPlugins
            // 
            chkSFSEPlugins.Dock = System.Windows.Forms.DockStyle.Fill;
            chkSFSEPlugins.FormattingEnabled = true;
            chkSFSEPlugins.Location = new System.Drawing.Point(3, 3);
            chkSFSEPlugins.Name = "chkSFSEPlugins";
            chkSFSEPlugins.Size = new System.Drawing.Size(866, 386);
            chkSFSEPlugins.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(chkSFSEPlugins, 0, 0);
            tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 1);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.Size = new System.Drawing.Size(872, 450);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.Controls.Add(btnSelectAll);
            flowLayoutPanel1.Controls.Add(btnSelectNone);
            flowLayoutPanel1.Controls.Add(btnOpenFolder);
            flowLayoutPanel1.Controls.Add(btnOk);
            flowLayoutPanel1.Controls.Add(btnCancel);
            flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel1.Location = new System.Drawing.Point(3, 395);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new System.Drawing.Size(866, 52);
            flowLayoutPanel1.TabIndex = 1;
            // 
            // btnSelectAll
            // 
            btnSelectAll.AutoSize = true;
            btnSelectAll.Location = new System.Drawing.Point(3, 3);
            btnSelectAll.Name = "btnSelectAll";
            btnSelectAll.Size = new System.Drawing.Size(150, 46);
            btnSelectAll.TabIndex = 1;
            btnSelectAll.Text = "Select All";
            btnSelectAll.UseVisualStyleBackColor = true;
            btnSelectAll.Click += btnSelectAll_Click;
            // 
            // btnSelectNone
            // 
            btnSelectNone.AutoSize = true;
            btnSelectNone.Location = new System.Drawing.Point(159, 3);
            btnSelectNone.Name = "btnSelectNone";
            btnSelectNone.Size = new System.Drawing.Size(154, 46);
            btnSelectNone.TabIndex = 2;
            btnSelectNone.Text = "Select None";
            btnSelectNone.UseVisualStyleBackColor = true;
            btnSelectNone.Click += btnSelectNone_Click;
            // 
            // btnOpenFolder
            // 
            btnOpenFolder.AutoSize = true;
            btnOpenFolder.Location = new System.Drawing.Point(319, 3);
            btnOpenFolder.Name = "btnOpenFolder";
            btnOpenFolder.Size = new System.Drawing.Size(165, 46);
            btnOpenFolder.TabIndex = 3;
            btnOpenFolder.Text = "Plugin Folder";
            btnOpenFolder.UseVisualStyleBackColor = true;
            btnOpenFolder.Click += btnOpenFolder_Click;
            // 
            // btnOk
            // 
            btnOk.AutoSize = true;
            btnOk.Location = new System.Drawing.Point(490, 3);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(150, 46);
            btnOk.TabIndex = 4;
            btnOk.Text = "Ok";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.Location = new System.Drawing.Point(646, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(150, 46);
            btnCancel.TabIndex = 5;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // frmSFSEPlugins
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(872, 450);
            Controls.Add(tableLayoutPanel1);
            Name = "frmSFSEPlugins";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "SFSE Plugins";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.CheckedListBox chkSFSEPlugins;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Button btnSelectNone;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOpenFolder;
    }
}