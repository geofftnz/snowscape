using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace Snowscape.Viewer
{
    public class TerrainViewer:GameWindow
    {

        public class CloseEventArgs : EventArgs { }
        public delegate void CloseEventHandler(object source, CloseEventArgs e);
        public event CloseEventHandler OnClose;

        public TerrainViewer():base(640,480,new GraphicsMode(),"Snowscape",GameWindowFlags.Default,DisplayDevice.Default,3,0,GraphicsContextFlags.Default)
        {

        }

        protected override void OnClosed(EventArgs e)
        {
            OnClose(this, new CloseEventArgs());
            base.OnClosed(e);
        }


        [STAThread]
        public static void Main()
        {
            using (var tv = new TerrainViewer())
            {
                tv.Run(30.0);
            }

            
        }
    }
}
