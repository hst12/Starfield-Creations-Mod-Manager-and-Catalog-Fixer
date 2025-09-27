﻿using CMM.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace CMM
{
    public partial class frmSplashScreen : Form
    {
        public frmSplashScreen()
        {
            InitializeComponent();
            string LoadScreen = Settings.Default.LoadScreenFilename;
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            float screenWidth;
            float screenHeight;

            if (LoadScreen != null && LoadScreen != "")
            {
                try
                {
                    var bitmap = new Bitmap(LoadScreen);
                    this.BackgroundImage = bitmap;
                }
                catch
                {
                    Settings.Default.LoadScreenFilename = "";
                    Settings.Default.Save();
                }
            }

            // Ensure the background image is already set in the designer
            Image backgroundImage = this.BackgroundImage;

            // Check if the background image exists
            if (backgroundImage != null)
            {
                // Get the screen resolution
                Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
                screenWidth = screenBounds.Width;
                screenHeight = screenBounds.Height;

                // Calculate the maximum allowed size for the longest side (75% of screen size)
                int maxLongestSide = (int)(0.75 * Math.Max(screenWidth, screenHeight));

                // Calculate the new dimensions while maintaining the aspect ratio
                float aspectRatio = (float)backgroundImage.Width / backgroundImage.Height;
                int newWidth, newHeight;

                if (backgroundImage.Width > backgroundImage.Height) // Landscape
                {
                    newWidth = (int)(backgroundImage.Width * .75);
                    if (newWidth > (screenWidth * 0.75))
                        newWidth = (int)(screenWidth * 0.75);
                    if (backgroundImage.Width < screenWidth * 0.75)
                        newWidth = (int)(screenWidth * 0.75);
                    newHeight = (int)(newWidth / aspectRatio);
                    if (newHeight > (screenHeight * 0.75))
                    {
                        newHeight = (int)(screenHeight * 0.75);
                        newWidth = (int)(newHeight * aspectRatio);
                    }
                }
                else // Portrait
                {
                    newHeight = (int)(0.75 * Math.Min(screenWidth, screenHeight));
                    newWidth = (int)(newHeight * aspectRatio);
                }

                // Set the form's client size to the calculated dimensions
                this.ClientSize = new Size(newWidth, newHeight);

                // Optional: Center the form on the screen
            }
            else
            {
                MessageBox.Show("No background image found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            this.StartPosition = FormStartPosition.CenterScreen;
        }
    }
}