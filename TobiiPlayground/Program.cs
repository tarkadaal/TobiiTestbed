using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TobiiPlayground
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
            using (var engine = new EyeTrackingEngine())
            {
                Application.Run(new DemoForm(engine));
            }
        }
    }
}
