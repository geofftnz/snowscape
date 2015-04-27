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
using OpenTKExtensions.Components;
using OpenTKExtensions.Camera;

namespace SDF
{
    public class SDFTestbench : GameWindow
    {
        private GameComponentCollection components = new GameComponentCollection();
        private GameComponentCollection Components { get { return components; } }

        #region Components
        private Font font; 
        private TextManager textManager;
        private FrameCounter frameCounter;
        private WalkCamera camera;
        #endregion

        private const string SHADERPATH = @"../../Resources/Shaders";

        public SDFTestbench()
            : base(
            800, 600,
            OpenTK.Graphics.GraphicsMode.Default,
            "SDF Testbench",
            GameWindowFlags.Default,
            DisplayDevice.Default,
            3, 1,
            OpenTK.Graphics.GraphicsContextFlags.Default
            )
        {
            this.VSync = OpenTK.VSyncMode.Off;

            // set default shader loader
            ShaderProgram.DefaultLoader = new OpenTKExtensions.Loaders.FileSystemLoader(SHADERPATH);

            Components.Add(font = new Font("Resources/consolab.ttf_sdf_512.png", "Resources/consolab.ttf_sdf_512.txt"));
            Components.Add(textManager = new TextManager("main", font) { AutoTransform = true });
            Components.Add(frameCounter = new FrameCounter());
            Components.Add(camera = new WalkCamera(this.Keyboard, this.Mouse));


            this.Load += SDFTestbench_Load;
            this.Unload += SDFTestbench_Unload;
            this.UpdateFrame += SDFTestbench_UpdateFrame;
            this.RenderFrame += SDFTestbench_RenderFrame;
            this.Resize += SDFTestbench_Resize;
        }

        private void SDFTestbench_Resize(object sender, EventArgs e)
        {
            GL.Viewport(this.ClientRectangle);
            Components.Resize(this.ClientRectangle.Width, this.ClientRectangle.Height);
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
            FrameData frame = new FrameData();

            GL.Viewport(this.ClientRectangle);
            GL.ClearColor(0.0f, 0.0f, 0.3f, 1.0f);
            GL.ClearDepth(1.0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            textManager.AddOrUpdate(frameCounter.TextBlock);

            float y = 0.1f;
            textManager.AddOrUpdate(new TextBlock("camera", string.Format("{0}", camera.EyePos.ToString()), new Vector3(0.0f, y, 0.0f), 0.0005f, Color4.Wheat.ToVector4())); y += 0.05f;

            Components.Render(frame);

            SwapBuffers();
            //Thread.Sleep(1);
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
