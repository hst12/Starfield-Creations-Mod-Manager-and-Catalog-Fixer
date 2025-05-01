using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Starfield_Tools
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Process currentProcess = Process.GetCurrentProcess();
            var runningProcesses = Process.GetProcessesByName(currentProcess.ProcessName);

            if (runningProcesses.Length > 1)
            {
                MessageBox.Show("Another instance is already running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.Run(new frmLoadOrder(""));
        }
    }
}
