using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace hstCMM
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Process currentProcess = Process.GetCurrentProcess();
            var runningProcesses = Process.GetProcessesByName(currentProcess.ProcessName);

            if (runningProcesses.Length > 1)
            {
                MessageBox.Show("Another instance is already running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Application.Run(new frmLoadOrder(""));
        }
    }
}
