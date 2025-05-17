namespace Starfield_Tools
{
    partial class frmStarfieldTools
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmStarfieldTools));
            richTextBox2 = new System.Windows.Forms.RichTextBox();
            richTextBox1 = new System.Windows.Forms.RichTextBox();
            btnClose = new System.Windows.Forms.Button();
            cmdClean = new System.Windows.Forms.Button();
            btnLoad = new System.Windows.Forms.Button();
            btnCheck = new System.Windows.Forms.Button();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            btnBackup = new System.Windows.Forms.Button();
            btnRestore = new System.Windows.Forms.Button();
            chkForceClean = new System.Windows.Forms.CheckBox();
            btnResetAll = new System.Windows.Forms.Button();
            cmdDeleteStale = new System.Windows.Forms.Button();
            chkVerbose = new System.Windows.Forms.CheckBox();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            grpActions = new System.Windows.Forms.GroupBox();
            flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            btnAchievemnts = new System.Windows.Forms.Button();
            btnDelete = new System.Windows.Forms.Button();
            btnClearLog = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            groupBox2 = new System.Windows.Forms.GroupBox();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            grpAuto = new System.Windows.Forms.GroupBox();
            flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            chkAutoCheck = new System.Windows.Forms.CheckBox();
            chkAutoClean = new System.Windows.Forms.CheckBox();
            chkAutoBackup = new System.Windows.Forms.CheckBox();
            chkAutoRestore = new System.Windows.Forms.CheckBox();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            statusStrip1.SuspendLayout();
            grpActions.SuspendLayout();
            flowLayoutPanel3.SuspendLayout();
            groupBox1.SuspendLayout();
            flowLayoutPanel4.SuspendLayout();
            groupBox2.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            grpAuto.SuspendLayout();
            flowLayoutPanel2.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // richTextBox2
            // 
            richTextBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            richTextBox2.Location = new System.Drawing.Point(3, 348);
            richTextBox2.Name = "richTextBox2";
            richTextBox2.ReadOnly = true;
            richTextBox2.Size = new System.Drawing.Size(1636, 339);
            richTextBox2.TabIndex = 1;
            richTextBox2.TabStop = false;
            richTextBox2.Text = "";
            // 
            // richTextBox1
            // 
            richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            richTextBox1.Location = new System.Drawing.Point(6, 6);
            richTextBox1.Margin = new System.Windows.Forms.Padding(6);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new System.Drawing.Size(1630, 333);
            richTextBox1.TabIndex = 0;
            richTextBox1.TabStop = false;
            richTextBox1.Text = "";
            // 
            // btnClose
            // 
            btnClose.AutoSize = true;
            btnClose.Location = new System.Drawing.Point(3, 3);
            btnClose.Name = "btnClose";
            btnClose.Size = new System.Drawing.Size(223, 50);
            btnClose.TabIndex = 20;
            btnClose.Text = "Close";
            toolTip1.SetToolTip(btnClose, "Quit");
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnQuit_Click;
            // 
            // cmdClean
            // 
            cmdClean.AutoSize = true;
            cmdClean.Location = new System.Drawing.Point(159, 3);
            cmdClean.Name = "cmdClean";
            cmdClean.Size = new System.Drawing.Size(150, 50);
            cmdClean.TabIndex = 1;
            cmdClean.Text = "Clean";
            toolTip1.SetToolTip(cmdClean, "Strip out corrupt characters");
            cmdClean.UseVisualStyleBackColor = true;
            cmdClean.Click += cmdClean_Click;
            // 
            // btnLoad
            // 
            btnLoad.AutoSize = true;
            btnLoad.Location = new System.Drawing.Point(3, 3);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new System.Drawing.Size(223, 50);
            btnLoad.TabIndex = 6;
            btnLoad.Text = "ContentCatalog.txt";
            toolTip1.SetToolTip(btnLoad, "Edit with default text editor");
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // btnCheck
            // 
            btnCheck.AutoSize = true;
            btnCheck.Location = new System.Drawing.Point(3, 3);
            btnCheck.Name = "btnCheck";
            btnCheck.Size = new System.Drawing.Size(150, 50);
            btnCheck.TabIndex = 0;
            btnCheck.Text = "Check";
            toolTip1.SetToolTip(btnCheck, "Check for corruption");
            btnCheck.UseVisualStyleBackColor = true;
            btnCheck.Click += btnCheck_Click;
            // 
            // btnBackup
            // 
            btnBackup.AutoSize = true;
            btnBackup.Location = new System.Drawing.Point(735, 3);
            btnBackup.Name = "btnBackup";
            btnBackup.Size = new System.Drawing.Size(150, 50);
            btnBackup.TabIndex = 4;
            btnBackup.Text = "Backup";
            toolTip1.SetToolTip(btnBackup, "Backup ContentCatalog.txt to ContentCatalog.txt.bak");
            btnBackup.UseVisualStyleBackColor = true;
            btnBackup.Click += btnBackup_Click;
            // 
            // btnRestore
            // 
            btnRestore.AutoSize = true;
            btnRestore.Location = new System.Drawing.Point(891, 3);
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new System.Drawing.Size(150, 50);
            btnRestore.TabIndex = 5;
            btnRestore.Text = "Restore";
            toolTip1.SetToolTip(btnRestore, "Restore ContentCatlog.txt.bak to ContextCatlog.txt");
            btnRestore.UseVisualStyleBackColor = true;
            btnRestore.Click += btnRestore_Click;
            // 
            // chkForceClean
            // 
            chkForceClean.AutoSize = true;
            chkForceClean.Location = new System.Drawing.Point(724, 3);
            chkForceClean.Name = "chkForceClean";
            chkForceClean.Size = new System.Drawing.Size(171, 36);
            chkForceClean.TabIndex = 14;
            chkForceClean.Text = "Force Clean";
            toolTip1.SetToolTip(chkForceClean, "Not normally needed");
            chkForceClean.UseVisualStyleBackColor = true;
            chkForceClean.CheckedChanged += chkForceClean_CheckedChanged;
            // 
            // btnResetAll
            // 
            btnResetAll.AutoSize = true;
            btnResetAll.Location = new System.Drawing.Point(519, 3);
            btnResetAll.Name = "btnResetAll";
            btnResetAll.Size = new System.Drawing.Size(210, 50);
            btnResetAll.TabIndex = 3;
            btnResetAll.Text = "Reset All Versions";
            toolTip1.SetToolTip(btnResetAll, "Resets all version numbers to 1704067200.0. Should enable updates");
            btnResetAll.UseVisualStyleBackColor = true;
            btnResetAll.Click += btnResetAll_Click;
            // 
            // cmdDeleteStale
            // 
            cmdDeleteStale.AutoSize = true;
            cmdDeleteStale.Location = new System.Drawing.Point(315, 3);
            cmdDeleteStale.Name = "cmdDeleteStale";
            cmdDeleteStale.Size = new System.Drawing.Size(198, 50);
            cmdDeleteStale.TabIndex = 2;
            cmdDeleteStale.Text = "Remove Unused";
            toolTip1.SetToolTip(cmdDeleteStale, "Remove missing mods from ContentCatalog.txt");
            cmdDeleteStale.UseVisualStyleBackColor = true;
            cmdDeleteStale.Click += cmdDeleteStale_Click;
            // 
            // chkVerbose
            // 
            chkVerbose.AutoSize = true;
            chkVerbose.Location = new System.Drawing.Point(901, 3);
            chkVerbose.Name = "chkVerbose";
            chkVerbose.Size = new System.Drawing.Size(242, 36);
            chkVerbose.TabIndex = 15;
            chkVerbose.Text = "Verbose Messages";
            toolTip1.SetToolTip(chkVerbose, "Display Info for Files Checked");
            chkVerbose.UseVisualStyleBackColor = true;
            chkVerbose.CheckedChanged += chkVerbose_CheckedChanged;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripStatusLabel1 });
            statusStrip1.Location = new System.Drawing.Point(0, 1100);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 14, 0);
            statusStrip1.Size = new System.Drawing.Size(1637, 42);
            statusStrip1.TabIndex = 5;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new System.Drawing.Size(131, 32);
            toolStripStatusLabel1.Text = "Starting up";
            // 
            // grpActions
            // 
            grpActions.AutoSize = true;
            grpActions.Controls.Add(flowLayoutPanel3);
            grpActions.Dock = System.Windows.Forms.DockStyle.Fill;
            grpActions.Location = new System.Drawing.Point(6, 696);
            grpActions.Margin = new System.Windows.Forms.Padding(6);
            grpActions.Name = "grpActions";
            grpActions.Padding = new System.Windows.Forms.Padding(6);
            grpActions.Size = new System.Drawing.Size(1630, 100);
            grpActions.TabIndex = 0;
            grpActions.TabStop = false;
            grpActions.Text = "Actions";
            // 
            // flowLayoutPanel3
            // 
            flowLayoutPanel3.AutoSize = true;
            flowLayoutPanel3.Controls.Add(btnCheck);
            flowLayoutPanel3.Controls.Add(cmdClean);
            flowLayoutPanel3.Controls.Add(cmdDeleteStale);
            flowLayoutPanel3.Controls.Add(btnResetAll);
            flowLayoutPanel3.Controls.Add(btnBackup);
            flowLayoutPanel3.Controls.Add(btnRestore);
            flowLayoutPanel3.Controls.Add(btnAchievemnts);
            flowLayoutPanel3.Controls.Add(btnDelete);
            flowLayoutPanel3.Controls.Add(btnClearLog);
            flowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel3.Location = new System.Drawing.Point(6, 38);
            flowLayoutPanel3.Name = "flowLayoutPanel3";
            flowLayoutPanel3.Size = new System.Drawing.Size(1618, 56);
            flowLayoutPanel3.TabIndex = 7;
            // 
            // btnAchievemnts
            // 
            btnAchievemnts.AutoSize = true;
            btnAchievemnts.Location = new System.Drawing.Point(1047, 3);
            btnAchievemnts.Name = "btnAchievemnts";
            btnAchievemnts.Size = new System.Drawing.Size(174, 50);
            btnAchievemnts.TabIndex = 7;
            btnAchievemnts.Text = "Achievements";
            btnAchievemnts.UseVisualStyleBackColor = true;
            btnAchievemnts.Click += btnAchievemnts_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new System.Drawing.Point(1227, 3);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new System.Drawing.Size(150, 46);
            btnDelete.TabIndex = 8;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnClearLog
            // 
            btnClearLog.AutoSize = true;
            btnClearLog.Location = new System.Drawing.Point(1383, 3);
            btnClearLog.Name = "btnClearLog";
            btnClearLog.Size = new System.Drawing.Size(150, 50);
            btnClearLog.TabIndex = 6;
            btnClearLog.Text = "Clear Log";
            btnClearLog.UseVisualStyleBackColor = true;
            btnClearLog.Click += btnClearLog_Click;
            // 
            // groupBox1
            // 
            groupBox1.AutoSize = true;
            groupBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            groupBox1.Controls.Add(flowLayoutPanel4);
            groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            groupBox1.Location = new System.Drawing.Point(6, 994);
            groupBox1.Margin = new System.Windows.Forms.Padding(6);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(6);
            groupBox1.Size = new System.Drawing.Size(1630, 100);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Other";
            // 
            // flowLayoutPanel4
            // 
            flowLayoutPanel4.AutoSize = true;
            flowLayoutPanel4.Controls.Add(btnClose);
            flowLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel4.Location = new System.Drawing.Point(6, 38);
            flowLayoutPanel4.Name = "flowLayoutPanel4";
            flowLayoutPanel4.Size = new System.Drawing.Size(1618, 56);
            flowLayoutPanel4.TabIndex = 22;
            // 
            // groupBox2
            // 
            groupBox2.AutoSize = true;
            groupBox2.Controls.Add(flowLayoutPanel1);
            groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            groupBox2.Location = new System.Drawing.Point(3, 805);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(1636, 94);
            groupBox2.TabIndex = 0;
            groupBox2.TabStop = false;
            groupBox2.Text = "Edit";
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.Controls.Add(btnLoad);
            flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel1.Location = new System.Drawing.Point(3, 35);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new System.Drawing.Size(1630, 56);
            flowLayoutPanel1.TabIndex = 10;
            // 
            // grpAuto
            // 
            grpAuto.AutoSize = true;
            grpAuto.Controls.Add(flowLayoutPanel2);
            grpAuto.Dock = System.Windows.Forms.DockStyle.Fill;
            grpAuto.Location = new System.Drawing.Point(3, 905);
            grpAuto.Name = "grpAuto";
            grpAuto.Size = new System.Drawing.Size(1636, 80);
            grpAuto.TabIndex = 0;
            grpAuto.TabStop = false;
            grpAuto.Text = "Auto Functions";
            // 
            // flowLayoutPanel2
            // 
            flowLayoutPanel2.AutoSize = true;
            flowLayoutPanel2.Controls.Add(chkAutoCheck);
            flowLayoutPanel2.Controls.Add(chkAutoClean);
            flowLayoutPanel2.Controls.Add(chkAutoBackup);
            flowLayoutPanel2.Controls.Add(chkAutoRestore);
            flowLayoutPanel2.Controls.Add(chkForceClean);
            flowLayoutPanel2.Controls.Add(chkVerbose);
            flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel2.Location = new System.Drawing.Point(3, 35);
            flowLayoutPanel2.Name = "flowLayoutPanel2";
            flowLayoutPanel2.Size = new System.Drawing.Size(1630, 42);
            flowLayoutPanel2.TabIndex = 15;
            // 
            // chkAutoCheck
            // 
            chkAutoCheck.AutoSize = true;
            chkAutoCheck.Location = new System.Drawing.Point(3, 3);
            chkAutoCheck.Name = "chkAutoCheck";
            chkAutoCheck.Size = new System.Drawing.Size(169, 36);
            chkAutoCheck.TabIndex = 10;
            chkAutoCheck.Text = "Auto Check";
            chkAutoCheck.UseVisualStyleBackColor = true;
            chkAutoCheck.CheckedChanged += chkAutoCheck_CheckedChanged;
            // 
            // chkAutoClean
            // 
            chkAutoClean.AutoSize = true;
            chkAutoClean.Location = new System.Drawing.Point(178, 3);
            chkAutoClean.Name = "chkAutoClean";
            chkAutoClean.Size = new System.Drawing.Size(164, 36);
            chkAutoClean.TabIndex = 11;
            chkAutoClean.Text = "Auto Clean";
            chkAutoClean.UseVisualStyleBackColor = true;
            chkAutoClean.CheckedChanged += chkAutoClean_CheckedChanged;
            // 
            // chkAutoBackup
            // 
            chkAutoBackup.AutoSize = true;
            chkAutoBackup.Location = new System.Drawing.Point(348, 3);
            chkAutoBackup.Name = "chkAutoBackup";
            chkAutoBackup.Size = new System.Drawing.Size(181, 36);
            chkAutoBackup.TabIndex = 12;
            chkAutoBackup.Text = "Auto Backup";
            chkAutoBackup.UseVisualStyleBackColor = true;
            chkAutoBackup.CheckedChanged += chkAutoBackup_CheckedChanged;
            // 
            // chkAutoRestore
            // 
            chkAutoRestore.AutoSize = true;
            chkAutoRestore.Location = new System.Drawing.Point(535, 3);
            chkAutoRestore.Name = "chkAutoRestore";
            chkAutoRestore.Size = new System.Drawing.Size(183, 36);
            chkAutoRestore.TabIndex = 13;
            chkAutoRestore.Text = "Auto Restore";
            chkAutoRestore.UseVisualStyleBackColor = true;
            chkAutoRestore.CheckedChanged += chkAutoRestore_CheckedChanged;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            tableLayoutPanel1.Controls.Add(richTextBox2, 0, 1);
            tableLayoutPanel1.Controls.Add(groupBox1, 0, 5);
            tableLayoutPanel1.Controls.Add(grpAuto, 0, 4);
            tableLayoutPanel1.Controls.Add(richTextBox1, 0, 0);
            tableLayoutPanel1.Controls.Add(groupBox2, 0, 3);
            tableLayoutPanel1.Controls.Add(grpActions, 0, 2);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 6;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.Size = new System.Drawing.Size(1637, 1100);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // frmStarfieldTools
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(192F, 192F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            ClientSize = new System.Drawing.Size(1637, 1142);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(statusStrip1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4);
            Name = "frmStarfieldTools";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Starfield Tools";
            FormClosing += frmStarfieldTools_FormClosing;
            Shown += frmStarfieldTools_Shown;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            grpActions.ResumeLayout(false);
            grpActions.PerformLayout();
            flowLayoutPanel3.ResumeLayout(false);
            flowLayoutPanel3.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            flowLayoutPanel4.ResumeLayout(false);
            flowLayoutPanel4.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            grpAuto.ResumeLayout(false);
            grpAuto.PerformLayout();
            flowLayoutPanel2.ResumeLayout(false);
            flowLayoutPanel2.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button cmdClean;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnCheck;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.GroupBox grpActions;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnRestore;
        private System.Windows.Forms.Button btnBackup;
        private System.Windows.Forms.GroupBox grpAuto;
        private System.Windows.Forms.CheckBox chkAutoBackup;
        private System.Windows.Forms.CheckBox chkAutoClean;
        private System.Windows.Forms.CheckBox chkAutoCheck;
        private System.Windows.Forms.CheckBox chkAutoRestore;
        private System.Windows.Forms.RichTextBox richTextBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.CheckBox chkForceClean;
        private System.Windows.Forms.Button cmdDeleteStale;
        private System.Windows.Forms.Button btnResetAll;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.CheckBox chkVerbose;
        private System.Windows.Forms.Button btnAchievemnts;
        private System.Windows.Forms.Button btnDelete;
    }
}

