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
using SDF.Renderers;
using OpenTK.Input;
using System.Diagnostics;
using System.IO;
using Utils;

namespace SDF
{
    public class SDFTestbench : GameWindow
    {
        private GameComponentCollection components = new GameComponentCollection();
        private GameComponentCollection Components { get { return components; } }

        private Dictionary<Key, Action<KeyModifiers>> keyDownActions = new Dictionary<Key, Action<KeyModifiers>>();
        private Dictionary<Key, Action<KeyModifiers>> keyUpActions = new Dictionary<Key, Action<KeyModifiers>>();

        private Stopwatch stopwatch = Stopwatch.StartNew();
        //private FileSystemWatcher shaderWatcher;
        private FileSystemPoller shaderWatcher;
        private long iterations = 0;
        private float wheel = 0.0f;

        #region Components
        private Font font;
        private TextManager textManager;
        private FrameCounter frameCounter;
        private WalkCamera camera;
        private SDFRenderer sdfRenderer;
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
            Components.Add(camera = new WalkCamera(this.Keyboard, this.Mouse) { MovementSpeed = 10.0f, Position = new Vector3(0f,1f,0f), LookMode = WalkCamera.LookModeEnum.Mouse1});
            Components.Add(sdfRenderer = new SDFRenderer() { Camera = camera });

            keyDownActions.Add(Key.Escape, (km) => { this.Close(); });
            keyDownActions.Add(Key.I, (km) => { sdfRenderer.ShowTraceDepth = true; });
            keyUpActions.Add(Key.I, (km) => { sdfRenderer.ShowTraceDepth = false; });

            this.Load += SDFTestbench_Load;
            this.Unload += SDFTestbench_Unload;
            this.UpdateFrame += SDFTestbench_UpdateFrame;
            this.RenderFrame += SDFTestbench_RenderFrame;
            this.Resize += SDFTestbench_Resize;

            this.KeyDown += SDFTestbench_KeyDown;
            this.KeyUp += SDFTestbench_KeyUp;

            //shaderWatcher = new FileSystemWatcher(SHADERPATH);
            //shaderWatcher.Changed += shaderWatcher_Changed;
            //shaderWatcher.EnableRaisingEvents = true;
            shaderWatcher = new FileSystemPoller(SHADERPATH);

            this.MouseWheel += SDFTestbench_MouseWheel;

        }

        void SDFTestbench_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            wheel += e.DeltaPrecise;
        }

        void SDFTestbench_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
        {
            var k = e.Key;
            var km = e.Modifiers;

            Action<KeyModifiers> action = null;
            if (keyDownActions.TryGetValue(e.Key, out action))
            {
                action(km);
            }
        }
        void SDFTestbench_KeyUp(object sender, KeyboardKeyEventArgs e)
        {
            var k = e.Key;
            var km = e.Modifiers;

            Action<KeyModifiers> action = null;
            if (keyUpActions.TryGetValue(e.Key, out action))
            {
                action(km);
            }
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

        void shaderWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Components.Reload();
        }


        void SDFTestbench_RenderFrame(object sender, FrameEventArgs e)
        {
            iterations++;
            FrameData frame = new FrameData() { Time = e.Time, Elapsed = stopwatch.Elapsed };
            sdfRenderer.Wheel = this.wheel;

            if ((iterations & 0x3f) == 0)
            {
                shaderWatcher.Poll();
                if (shaderWatcher.HasChanges)
                {
                    this.Components.Reload();
                    shaderWatcher.Reset();
                }
            }

            GL.Viewport(this.ClientRectangle);
            //GL.ClearColor(0.0f, 0.0f, 0.3f, 1.0f);
            GL.ClearDepth(1.0);
            GL.Clear(ClearBufferMask.DepthBufferBit); 
            //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            textManager.AddOrUpdate(frameCounter.TextBlock);

            float y = 0.1f;
            textManager.AddOrUpdate(new TextBlock("camera", string.Format("{0}", camera.EyePos.ToString()), new Vector3(0.0f, y, 0.0f), 0.0005f, Color4.Wheat.ToVector4())); y += 0.05f;

            Components.Render(frame);

            SwapBuffers();
            Thread.Sleep(0);
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
