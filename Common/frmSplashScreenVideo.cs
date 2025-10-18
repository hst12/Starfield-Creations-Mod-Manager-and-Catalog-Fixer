using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace hstCMM.Common
{
    public partial class frmSplashScreenVideo : Form
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;

        public frmSplashScreenVideo()
        {
            InitializeComponent();
            Core.Initialize(); // Required for LibVLCSharp

            // this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;
            Rectangle resolution = Screen.PrimaryScreen.Bounds; // Resize window to 85% of screen width
            double screenWidth = resolution.Width;
            double screenHeight = resolution.Height;
            this.Width = (int)(screenWidth * 0.85);
            this.Height = (int)(screenHeight * 0.85);
            this.StartPosition = FormStartPosition.CenterScreen;
            var videoView = new VideoView
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(videoView);

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            videoView.MediaPlayer = _mediaPlayer;

            var media = new Media(_libVLC, @"D:\Andre\Videos\Starfield\Rotating View.mp4", FromType.FromPath);
            _mediaPlayer.Play(media);

            _mediaPlayer.EndReached += (sender, args) =>
            {
                this.Invoke(new Action(() => this.Close()));
            };
        }
    }
}