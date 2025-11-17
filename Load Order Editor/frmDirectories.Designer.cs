namespace hstCMM.Load_Order_Editor
{
    partial class frmDirectories
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
            btnLOOT = new System.Windows.Forms.Button();
            btnMO2 = new System.Windows.Forms.Button();
            btnVortex = new System.Windows.Forms.Button();
            btnxEdit = new System.Windows.Forms.Button();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            btnFont = new System.Windows.Forms.Button();
            btnOk = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            btnGame = new System.Windows.Forms.Button();
            txtGame = new System.Windows.Forms.TextBox();
            txtLOOT = new System.Windows.Forms.TextBox();
            txtMO2 = new System.Windows.Forms.TextBox();
            txtVortex = new System.Windows.Forms.TextBox();
            txtxEdit = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            btnDetect = new System.Windows.Forms.Button();
            fontDialog1 = new System.Windows.Forms.FontDialog();
            openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // btnLOOT
            // 
            btnLOOT.AutoSize = true;
            btnLOOT.Location = new System.Drawing.Point(3, 111);
            btnLOOT.Name = "btnLOOT";
            btnLOOT.Size = new System.Drawing.Size(209, 42);
            btnLOOT.TabIndex = 2;
            btnLOOT.Text = "LOOT";
            btnLOOT.UseVisualStyleBackColor = true;
            btnLOOT.Click += btnLOOT_Click;
            // 
            // btnMO2
            // 
            btnMO2.AutoSize = true;
            btnMO2.Location = new System.Drawing.Point(3, 165);
            btnMO2.Name = "btnMO2";
            btnMO2.Size = new System.Drawing.Size(209, 42);
            btnMO2.TabIndex = 4;
            btnMO2.Text = "Mod Organzser 2";
            btnMO2.UseVisualStyleBackColor = true;
            btnMO2.Click += btnMO2_Click;
            // 
            // btnVortex
            // 
            btnVortex.AutoSize = true;
            btnVortex.Location = new System.Drawing.Point(3, 219);
            btnVortex.Name = "btnVortex";
            btnVortex.Size = new System.Drawing.Size(209, 42);
            btnVortex.TabIndex = 6;
            btnVortex.Text = "Vortex";
            btnVortex.UseVisualStyleBackColor = true;
            btnVortex.Click += btnVortex_Click;
            // 
            // btnxEdit
            // 
            btnxEdit.AutoSize = true;
            btnxEdit.Location = new System.Drawing.Point(3, 273);
            btnxEdit.Name = "btnxEdit";
            btnxEdit.Size = new System.Drawing.Size(209, 42);
            btnxEdit.TabIndex = 8;
            btnxEdit.Text = "xEdit";
            btnxEdit.UseVisualStyleBackColor = true;
            btnxEdit.Click += btnxEdit_Click;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            tableLayoutPanel1.Controls.Add(btnFont, 0, 7);
            tableLayoutPanel1.Controls.Add(btnOk, 0, 9);
            tableLayoutPanel1.Controls.Add(btnCancel, 1, 9);
            tableLayoutPanel1.Controls.Add(btnGame, 0, 1);
            tableLayoutPanel1.Controls.Add(txtGame, 1, 1);
            tableLayoutPanel1.Controls.Add(btnxEdit, 0, 5);
            tableLayoutPanel1.Controls.Add(btnVortex, 0, 4);
            tableLayoutPanel1.Controls.Add(btnLOOT, 0, 2);
            tableLayoutPanel1.Controls.Add(btnMO2, 0, 3);
            tableLayoutPanel1.Controls.Add(txtLOOT, 1, 2);
            tableLayoutPanel1.Controls.Add(txtMO2, 1, 3);
            tableLayoutPanel1.Controls.Add(txtVortex, 1, 4);
            tableLayoutPanel1.Controls.Add(txtxEdit, 1, 5);
            tableLayoutPanel1.Controls.Add(label1, 0, 0);
            tableLayoutPanel1.Controls.Add(label2, 0, 6);
            tableLayoutPanel1.Controls.Add(btnDetect, 1, 7);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 10;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            tableLayoutPanel1.Size = new System.Drawing.Size(1062, 548);
            tableLayoutPanel1.TabIndex = 5;
            // 
            // btnFont
            // 
            btnFont.AutoSize = true;
            btnFont.Location = new System.Drawing.Point(3, 381);
            btnFont.Name = "btnFont";
            btnFont.Size = new System.Drawing.Size(209, 42);
            btnFont.TabIndex = 10;
            btnFont.Text = "Font Size";
            btnFont.UseVisualStyleBackColor = true;
            btnFont.Click += btnFont_Click;
            // 
            // btnOk
            // 
            btnOk.Location = new System.Drawing.Point(3, 489);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(209, 46);
            btnOk.TabIndex = 11;
            btnOk.Text = "Ok";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new System.Drawing.Point(218, 489);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(209, 46);
            btnCancel.TabIndex = 12;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnGame
            // 
            btnGame.AutoSize = true;
            btnGame.Location = new System.Drawing.Point(3, 57);
            btnGame.Name = "btnGame";
            btnGame.Size = new System.Drawing.Size(209, 42);
            btnGame.TabIndex = 0;
            btnGame.Text = "Game";
            btnGame.UseVisualStyleBackColor = true;
            btnGame.Click += btnGame_Click;
            // 
            // txtGame
            // 
            txtGame.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtGame.Location = new System.Drawing.Point(218, 57);
            txtGame.Name = "txtGame";
            txtGame.Size = new System.Drawing.Size(841, 39);
            txtGame.TabIndex = 1;
            // 
            // txtLOOT
            // 
            txtLOOT.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtLOOT.Location = new System.Drawing.Point(218, 111);
            txtLOOT.Name = "txtLOOT";
            txtLOOT.Size = new System.Drawing.Size(841, 39);
            txtLOOT.TabIndex = 3;
            // 
            // txtMO2
            // 
            txtMO2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtMO2.Location = new System.Drawing.Point(218, 165);
            txtMO2.Name = "txtMO2";
            txtMO2.Size = new System.Drawing.Size(841, 39);
            txtMO2.TabIndex = 5;
            // 
            // txtVortex
            // 
            txtVortex.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtVortex.Location = new System.Drawing.Point(218, 219);
            txtVortex.Name = "txtVortex";
            txtVortex.Size = new System.Drawing.Size(841, 39);
            txtVortex.TabIndex = 7;
            // 
            // txtxEdit
            // 
            txtxEdit.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtxEdit.Location = new System.Drawing.Point(218, 273);
            txtxEdit.Name = "txtxEdit";
            txtxEdit.Size = new System.Drawing.Size(841, 39);
            txtxEdit.TabIndex = 9;
            // 
            // label1
            // 
            label1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            label1.Location = new System.Drawing.Point(3, 22);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(209, 32);
            label1.TabIndex = 11;
            label1.Text = "Directories";
            label1.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // label2
            // 
            label2.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            label2.Location = new System.Drawing.Point(3, 346);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(209, 32);
            label2.TabIndex = 12;
            label2.Text = "Other Options";
            label2.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // btnDetect
            // 
            btnDetect.AutoSize = true;
            btnDetect.Location = new System.Drawing.Point(218, 381);
            btnDetect.Name = "btnDetect";
            btnDetect.Size = new System.Drawing.Size(363, 42);
            btnDetect.TabIndex = 13;
            btnDetect.Text = "Detect Game Path - Steam Only";
            btnDetect.UseVisualStyleBackColor = true;
            btnDetect.Click += btnDetect_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // frmDirectories
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1062, 548);
            Controls.Add(tableLayoutPanel1);
            Name = "frmDirectories";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Options";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button btnLOOT;
        private System.Windows.Forms.Button btnMO2;
        private System.Windows.Forms.Button btnVortex;
        private System.Windows.Forms.Button btnxEdit;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox txtxEdit;
        private System.Windows.Forms.TextBox txtGame;
        private System.Windows.Forms.TextBox txtLOOT;
        private System.Windows.Forms.TextBox txtMO2;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnGame;
        private System.Windows.Forms.TextBox txtVortex;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnFont;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.FontDialog fontDialog1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnDetect;
    }
}