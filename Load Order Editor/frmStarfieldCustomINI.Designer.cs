namespace Starfield_Tools.Load_Order_Editor
{
    partial class frmStarfieldCustomINI
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
            btnOK = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            chkLooseFiles = new System.Windows.Forms.CheckBox();
            chkMOTD = new System.Windows.Forms.CheckBox();
            chkPapyrusLogging = new System.Windows.Forms.CheckBox();
            chkUserPhotos = new System.Windows.Forms.CheckBox();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            btnSuggested = new System.Windows.Forms.Button();
            chkSkipIntro = new System.Windows.Forms.CheckBox();
            chkMainMenuDelay = new System.Windows.Forms.CheckBox();
            label1 = new System.Windows.Forms.Label();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // btnOK
            // 
            btnOK.AutoSize = true;
            btnOK.Dock = System.Windows.Forms.DockStyle.Left;
            btnOK.Location = new System.Drawing.Point(3, 3);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(130, 59);
            btnOK.TabIndex = 0;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.Dock = System.Windows.Forms.DockStyle.Right;
            btnCancel.Location = new System.Drawing.Point(605, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(136, 59);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // chkLooseFiles
            // 
            chkLooseFiles.AutoSize = true;
            chkLooseFiles.Location = new System.Drawing.Point(12, 12);
            chkLooseFiles.Name = "chkLooseFiles";
            chkLooseFiles.Size = new System.Drawing.Size(162, 36);
            chkLooseFiles.TabIndex = 2;
            chkLooseFiles.Text = "Loose Files";
            chkLooseFiles.UseVisualStyleBackColor = true;
            // 
            // chkMOTD
            // 
            chkMOTD.AutoSize = true;
            chkMOTD.Location = new System.Drawing.Point(12, 54);
            chkMOTD.Name = "chkMOTD";
            chkMOTD.Size = new System.Drawing.Size(361, 36);
            chkMOTD.TabIndex = 3;
            chkMOTD.Text = "Disable Message of the Day *";
            chkMOTD.UseVisualStyleBackColor = true;
            // 
            // chkPapyrusLogging
            // 
            chkPapyrusLogging.AutoSize = true;
            chkPapyrusLogging.Location = new System.Drawing.Point(12, 222);
            chkPapyrusLogging.Name = "chkPapyrusLogging";
            chkPapyrusLogging.Size = new System.Drawing.Size(222, 36);
            chkPapyrusLogging.TabIndex = 4;
            chkPapyrusLogging.Text = "Papyrus Logging";
            chkPapyrusLogging.UseVisualStyleBackColor = true;
            // 
            // chkUserPhotos
            // 
            chkUserPhotos.AutoSize = true;
            chkUserPhotos.Location = new System.Drawing.Point(12, 96);
            chkUserPhotos.Name = "chkUserPhotos";
            chkUserPhotos.Size = new System.Drawing.Size(334, 36);
            chkUserPhotos.TabIndex = 5;
            chkUserPhotos.Text = "Player Photo Loadscreens *";
            chkUserPhotos.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.Controls.Add(btnOK, 0, 0);
            tableLayoutPanel1.Controls.Add(btnCancel, 2, 0);
            tableLayoutPanel1.Controls.Add(btnSuggested, 1, 0);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 353);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new System.Drawing.Size(744, 65);
            tableLayoutPanel1.TabIndex = 6;
            // 
            // btnSuggested
            // 
            btnSuggested.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnSuggested.AutoSize = true;
            btnSuggested.Location = new System.Drawing.Point(263, 3);
            btnSuggested.Name = "btnSuggested";
            btnSuggested.Size = new System.Drawing.Size(230, 59);
            btnSuggested.TabIndex = 2;
            btnSuggested.Text = "Suggested Settings";
            btnSuggested.UseVisualStyleBackColor = true;
            btnSuggested.Click += btnSuggested_Click;
            // 
            // chkSkipIntro
            // 
            chkSkipIntro.AutoSize = true;
            chkSkipIntro.Location = new System.Drawing.Point(12, 180);
            chkSkipIntro.Name = "chkSkipIntro";
            chkSkipIntro.Size = new System.Drawing.Size(148, 36);
            chkSkipIntro.TabIndex = 7;
            chkSkipIntro.Text = "Skip Intro";
            chkSkipIntro.UseVisualStyleBackColor = true;
            // 
            // chkMainMenuDelay
            // 
            chkMainMenuDelay.AutoSize = true;
            chkMainMenuDelay.Location = new System.Drawing.Point(12, 138);
            chkMainMenuDelay.Name = "chkMainMenuDelay";
            chkMainMenuDelay.Size = new System.Drawing.Size(451, 36);
            chkMainMenuDelay.TabIndex = 8;
            chkMainMenuDelay.Text = "Reduce Main Menu Delay Before Skip";
            chkMainMenuDelay.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 296);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(703, 32);
            label1.TabIndex = 9;
            label1.Text = "* These options used together will disable all photo load screens";
            // 
            // frmStarfieldCustomINI
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(744, 418);
            Controls.Add(label1);
            Controls.Add(chkMainMenuDelay);
            Controls.Add(chkSkipIntro);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(chkUserPhotos);
            Controls.Add(chkPapyrusLogging);
            Controls.Add(chkMOTD);
            Controls.Add(chkLooseFiles);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmStarfieldCustomINI";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "StarfieldCustom.ini Settings (Caution - Overwrites file)";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox chkLooseFiles;
        private System.Windows.Forms.CheckBox chkMOTD;
        private System.Windows.Forms.CheckBox chkPapyrusLogging;
        private System.Windows.Forms.CheckBox chkUserPhotos;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.CheckBox chkSkipIntro;
        private System.Windows.Forms.CheckBox chkMainMenuDelay;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSuggested;
    }
}