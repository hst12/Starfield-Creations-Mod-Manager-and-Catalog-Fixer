namespace CMM.Common
{
    partial class frmLoading
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
            txtMessage = new System.Windows.Forms.TextBox();
            SuspendLayout();
            // 
            // txtMessage
            // 
            txtMessage.BackColor = System.Drawing.SystemColors.Control;
            txtMessage.Font = new System.Drawing.Font("Segoe UI", 12F);
            txtMessage.Location = new System.Drawing.Point(0, 0);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new System.Drawing.Size(246, 50);
            txtMessage.TabIndex = 0;
            txtMessage.Text = "Loading...";
            txtMessage.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // frmLoading
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            ClientSize = new System.Drawing.Size(273, 59);
            ControlBox = false;
            Controls.Add(txtMessage);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmLoading";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            TopMost = true;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtMessage;
    }
}