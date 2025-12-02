using System;
using System.Windows.Forms;
using Bismillah_FINAL_ForceBite;

namespace ForcebiteAnalyzer
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}