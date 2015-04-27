using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTKExtensions.Framework;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using System.Threading;
using OpenTKExtensions.Text;

namespace SDF
{
    public class SDFTestbench : GameWindow
    {
        private GameComponentCollection components = new GameComponentCollection();
        private GameComponentCollection Components { get { return components; } }

        #region Components
        private Font font = new Font("Resources/consolab.ttf_sdf_512.png", "Resources/consolab.ttf_sdf_512.txt");
        #endregion


        public SDFTestbench()
            : base(
            800, 600,
            OpenTK.Graphics.GraphicsMode.Default,
            "SDF Testbench",
            GameWindowFlags.Default,
            DisplayDevice.Default,
            3, 2,
            OpenTK.Graphics.GraphicsContextFlags.Default
            )
        {

            Components.Add(font);


            this.Load += SDFTestbench_Load;
            this.Unload += SDFTestbench_Unload;
            this.UpdateFrame += SDFTestbench_UpdateFrame;
            this.RenderFrame += SDFTestbench_RenderFrame;
        }


        void SDFTestbench_Load(object sender, EventArgs e)
        {
            Components.Load();
        }
        void SDFTestbench_Unload(object sender, EventArgs e)
        {
            Components.Unload();
        }


        void SDFTestbench_RenderFrame(object sender, FrameEventArgs e)
        {
            GL.ClearColor(Color4.DarkBlue);
            GL.ClearDepth(1.0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            SwapBuffers();
            Thread.Sleep(1);
        }

        void SDFTestbench_UpdateFrame(object sender, FrameEventArgs e)
        {
            var d = new FrameData
            {
                Time = e.Time
            };
            Components.Update(d);
        }
    }
}
