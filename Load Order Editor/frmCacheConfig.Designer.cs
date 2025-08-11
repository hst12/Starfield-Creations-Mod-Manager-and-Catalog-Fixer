namespace Starfield_Tools.Load_Order_Editor
{
    partial class frmCacheConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmCacheConfig));
            btnOk = new System.Windows.Forms.Button();
            btnClose = new System.Windows.Forms.Button();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            chkWwise = new System.Windows.Forms.CheckBox();
            chkMesh = new System.Windows.Forms.CheckBox();
            chkLOD = new System.Windows.Forms.CheckBox();
            chkTextures = new System.Windows.Forms.CheckBox();
            txtCacheSize = new System.Windows.Forms.TextBox();
            flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            flowLayoutPanel1.SuspendLayout();
            flowLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // btnOk
            // 
            btnOk.AutoSize = true;
            btnOk.Location = new System.Drawing.Point(3, 3);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(150, 46);
            btnOk.TabIndex = 0;
            btnOk.Text = "Generate";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnClose
            // 
            btnClose.AutoSize = true;
            btnClose.Location = new System.Drawing.Point(159, 3);
            btnClose.Name = "btnClose";
            btnClose.Size = new System.Drawing.Size(150, 46);
            btnClose.TabIndex = 1;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(chkWwise);
            flowLayoutPanel1.Controls.Add(chkMesh);
            flowLayoutPanel1.Controls.Add(chkLOD);
            flowLayoutPanel1.Controls.Add(chkTextures);
            flowLayoutPanel1.Controls.Add(txtCacheSize);
            flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            flowLayoutPanel1.Location = new System.Drawing.Point(12, 12);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new System.Drawing.Size(776, 374);
            flowLayoutPanel1.TabIndex = 2;
            // 
            // chkWwise
            // 
            chkWwise.AutoSize = true;
            chkWwise.Location = new System.Drawing.Point(3, 3);
            chkWwise.Name = "chkWwise";
            chkWwise.Size = new System.Drawing.Size(114, 36);
            chkWwise.TabIndex = 0;
            chkWwise.Text = "Wwise";
            chkWwise.UseVisualStyleBackColor = true;
            chkWwise.Click += chkWwise_Click;
            // 
            // chkMesh
            // 
            chkMesh.AutoSize = true;
            chkMesh.Location = new System.Drawing.Point(3, 45);
            chkMesh.Name = "chkMesh";
            chkMesh.Size = new System.Drawing.Size(105, 36);
            chkMesh.TabIndex = 1;
            chkMesh.Text = "Mesh";
            chkMesh.UseVisualStyleBackColor = true;
            chkMesh.Click += chkMesh_Click;
            // 
            // chkLOD
            // 
            chkLOD.AutoSize = true;
            chkLOD.Location = new System.Drawing.Point(3, 87);
            chkLOD.Name = "chkLOD";
            chkLOD.Size = new System.Drawing.Size(91, 36);
            chkLOD.TabIndex = 2;
            chkLOD.Text = "LOD";
            chkLOD.UseVisualStyleBackColor = true;
            chkLOD.Click += chkLOD_Click;
            // 
            // chkTextures
            // 
            chkTextures.AutoSize = true;
            chkTextures.Location = new System.Drawing.Point(3, 129);
            chkTextures.Name = "chkTextures";
            chkTextures.Size = new System.Drawing.Size(134, 36);
            chkTextures.TabIndex = 3;
            chkTextures.Text = "Textures";
            chkTextures.UseVisualStyleBackColor = true;
            chkTextures.Click += chkTextures_Click;
            // 
            // txtCacheSize
            // 
            txtCacheSize.Location = new System.Drawing.Point(3, 171);
            txtCacheSize.Name = "txtCacheSize";
            txtCacheSize.Size = new System.Drawing.Size(200, 39);
            txtCacheSize.TabIndex = 4;
            // 
            // flowLayoutPanel2
            // 
            flowLayoutPanel2.Controls.Add(btnOk);
            flowLayoutPanel2.Controls.Add(btnClose);
            flowLayoutPanel2.Location = new System.Drawing.Point(11, 402);
            flowLayoutPanel2.Name = "flowLayoutPanel2";
            flowLayoutPanel2.Size = new System.Drawing.Size(777, 67);
            flowLayoutPanel2.TabIndex = 5;
            // 
            // frmCacheConfig
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(809, 492);
            Controls.Add(flowLayoutPanel2);
            Controls.Add(flowLayoutPanel1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "frmCacheConfig";
            Text = "Cache Config";
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            flowLayoutPanel2.ResumeLayout(false);
            flowLayoutPanel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.CheckBox chkWwise;
        private System.Windows.Forms.CheckBox chkMesh;
        private System.Windows.Forms.CheckBox chkLOD;
        private System.Windows.Forms.CheckBox chkTextures;
        private System.Windows.Forms.TextBox txtCacheSize;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
    }
}