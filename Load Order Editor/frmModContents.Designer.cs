namespace Starfield_Tools.Load_Order_Editor
{
    partial class frmModContents
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmModContents));
            btnOK = new System.Windows.Forms.Button();
            richTextBox1 = new System.Windows.Forms.RichTextBox();
            btnShowArchives = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // btnOK
            // 
            btnOK.Location = new System.Drawing.Point(638, 392);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(150, 46);
            btnOK.TabIndex = 0;
            btnOK.Text = "Ok";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new System.Drawing.Point(12, 12);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new System.Drawing.Size(776, 356);
            richTextBox1.TabIndex = 1;
            richTextBox1.Text = "";
            // 
            // btnShowArchives
            // 
            btnShowArchives.Location = new System.Drawing.Point(12, 392);
            btnShowArchives.Name = "btnShowArchives";
            btnShowArchives.Size = new System.Drawing.Size(209, 46);
            btnShowArchives.TabIndex = 2;
            btnShowArchives.Text = "Archive Contents";
            btnShowArchives.UseVisualStyleBackColor = true;
            btnShowArchives.Click += btnShowArchives_Click;
            // 
            // frmModContents
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 450);
            Controls.Add(btnShowArchives);
            Controls.Add(richTextBox1);
            Controls.Add(btnOK);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "frmModContents";
            Text = "Mod Contents";
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button btnShowArchives;
    }
}