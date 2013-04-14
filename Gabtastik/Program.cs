using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Gabtastik
{
    static class Program
    {       
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            System.Diagnostics.Process[] RunningProcesses = System.Diagnostics.Process.GetProcessesByName("Gabtastik");

            if (RunningProcesses.Length == 1)
                Application.Run(new Form1());
        }
    }
}