﻿using Starfield_Tools.Properties;

namespace Starfield_Tools
{
    partial class frmSplashScreen
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
            SuspendLayout();
            // 
            // frmSplashScreen
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoSize = true;
            BackgroundImage = Resources.Photo_2024_07_26_095212;
            BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            ClientSize = new System.Drawing.Size(1339, 754);
            DoubleBuffered = true;
            Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            ForeColor = System.Drawing.SystemColors.ActiveBorder;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmSplashScreen";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Launching Starfield";
            ResumeLayout(false);
        }

        #endregion
    }
}