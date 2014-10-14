using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using Snowscape.Viewer;

namespace Snowscape
{
    public partial class Startup : Form
    {
        public Startup()
        {
            InitializeComponent();
        }

        private void RunViewerButton_Click(object sender, EventArgs e)
        {
            this.Hide();

            //using (var v = new Snowscape.Viewer.TerrainViewer())
            //{
            //    v.Run(30);
            //}
            this.Show();
        }

        private void RunGeneratorButton_Click(object sender, EventArgs e)
        {
            this.Hide();

            try
            {
                using (var v = new Snowscape.TerrainGenerationViewer.TerrainGenerationViewer())
                {
                    v.Run(60);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name);
                throw;
            }
            this.Show();
        }
    }
}
