using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;
using NLog;
using Utils;
using OpenTKExtensions.Camera;
using OpenTK.Input;


namespace Snowscape.Viewer
{
    public class TerrainViewer:GameWindow
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private Matrix4 overlayProjection = Matrix4.Identity;
        private Matrix4 overlayModelview = Matrix4.Identity;

        private Matrix4 terrainProjection = Matrix4.Identity;
        private Matrix4 terrainModelview = Matrix4.Identity;

        private Font font = new Font();
        private TextManager textManager = new TextManager("DefaultText", null);

        private FrameCounter frameCounter = new FrameCounter();
        private TextBlock frameCounterText = new TextBlock("fps", "", new Vector3(0.01f, 0.05f, 0.0f), 0.0005f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        private PerfMonitor perfmon = new PerfMonitor();

        private ICamera camera;


        public class CloseEventArgs : EventArgs { }
        public delegate void CloseEventHandler(object source, CloseEventArgs e);
        public event CloseEventHandler OnClose;

        public TerrainViewer():base(640,480,new GraphicsMode(),"Snowscape",GameWindowFlags.Default,DisplayDevice.Default,3,0,GraphicsContextFlags.Default)
        {
            this.Load += new EventHandler<EventArgs>(TerrainViewer_Load);
            this.Closed += new EventHandler<EventArgs>(TerrainViewer_Closed);
            this.UpdateFrame += new EventHandler<FrameEventArgs>(TerrainViewer_UpdateFrame);
            this.RenderFrame += new EventHandler<FrameEventArgs>(TerrainViewer_RenderFrame);
            this.Resize += new EventHandler<EventArgs>(TerrainViewer_Resize);
        }

        void TerrainViewer_Resize(object sender, EventArgs e)
        {
            SetProjection();
        }

        private void SetProjection()
        {
            GL.Viewport(this.ClientRectangle);
            SetOverlayProjection();
            SetTerrainProjection();
        }

        private void SetOverlayProjection()
        {
            this.overlayProjection = Matrix4.CreateOrthographicOffCenter(0.0f, (float)this.ClientRectangle.Width / (float)this.ClientRectangle.Height, 1.0f, 0.0f, 0.001f, 10.0f);
            this.overlayModelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);
        }

        private void SetTerrainProjection()
        {
            this.terrainProjection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI * 0.5f, (float)this.ClientRectangle.Width / (float)this.ClientRectangle.Height, 0.01f, 10.0f);
            //this.terrainModelview = Matrix4.LookAt(
        }

        void TerrainViewer_RenderFrame(object sender, FrameEventArgs e)
        {

            if (this.frameCounter.Frames % 32 == 0)
            {
                frameCounterText.Text = string.Format("FPS: {0:0}", frameCounter.FPSSmooth);
                textManager.AddOrUpdate(frameCounterText);

                float y = 0.1f;
                foreach (var timer in this.perfmon.AllAverageTimes())
                {
                    textManager.AddOrUpdate(new TextBlock("perf" + timer.Item1, string.Format("{0}: {1:0.000} ms", timer.Item1, timer.Item2), new Vector3(0.01f, y, 0.0f), 0.00025f, new Vector4(1.0f, 1.0f, 1.0f, 0.5f)));
                    y += 0.0125f;
                }
            }

            this.camera.GetProjectionMatrix(out this.terrainProjection);
            this.camera.GetModelviewMatrix(out this.terrainModelview);

            textManager.AddOrUpdate(new TextBlock("pmat", this.terrainProjection.ToString(), new Vector3(0.01f, 0.2f, 0.0f), 0.0005f, new Vector4(1.0f, 1.0f, 1.0f, 0.5f)));
            textManager.AddOrUpdate(new TextBlock("mvmat", this.terrainModelview.ToString(), new Vector3(0.01f, 0.25f, 0.0f), 0.0005f, new Vector4(1.0f, 1.0f, 1.0f, 0.5f)));

            GL.ClearColor(0.0f,0.0f,0.3f,1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            GL.Disable(EnableCap.DepthTest);
            perfmon.Start("RefreshText");
            if (textManager.NeedsRefresh) textManager.Refresh();
            perfmon.Stop("RefreshText");

            perfmon.Start("RenderText");
            textManager.Render(overlayProjection, overlayModelview);
            perfmon.Stop("RenderText");
            GL.Enable(EnableCap.DepthTest);

            GL.Flush();
            SwapBuffers();

            this.frameCounter.Frame();
        }

        void TerrainViewer_UpdateFrame(object sender, FrameEventArgs e)
        {
            this.camera.Update(e.Time);

            if (Keyboard[Key.Escape])
            {
                this.Close();
            }
        }

        void TerrainViewer_Closed(object sender, EventArgs e)
        {
            OnClose(this, new CloseEventArgs());
        }

        void TerrainViewer_Load(object sender, EventArgs e)
        {
            this.VSync = VSyncMode.Off;

            // create VBOs/Shaders etc

            // GL state
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(new Color4(0, 24, 64, 255));


            // setup font
            font.Init(Resources.FontConsolas, Resources.FontConsolasMeta);
            textManager.Font = font;

            // setup camera
            this.camera = new QuaternionCamera(Mouse, Keyboard, this);

            this.frameCounter.Start();
        }



    }
}
