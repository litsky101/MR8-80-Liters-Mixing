using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MR_8_80_Liters_Mixing_System;
namespace Subdivision
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            if(!Configuration.IsRunning())
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Subdivision());
            }
            else
            {
                MessageDisplay.Error("Application is already running");
            }
        }
    }
}
