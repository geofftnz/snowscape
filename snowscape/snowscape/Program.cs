using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Windows.Forms;
using NLog;

namespace Snowscape
{
    static class Program
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        static void Main()
        {
            log.Info("Snowscape START");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Startup());

            try
            {
                using (var v = new Snowscape.TerrainGenerationViewer.TerrainGenerationViewer())
                {
                    v.Run(60);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, ex.GetType().Name);
                throw;
            }


            log.Info("Snowscape END");
        }
    }
}
