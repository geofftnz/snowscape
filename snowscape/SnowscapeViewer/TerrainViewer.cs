﻿using System;
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
using Snowscape.TerrainRenderer;
using Snowscape.TerrainRenderer.Renderers;
using OpenTKExtensions.Framework;

namespace Snowscape.Viewer
{
    public class TerrainViewer : GameWindow
    {
        const int TILESIZE = 256;

        private static Logger log = LogManager.GetCurrentClassLogger();

        private Matrix4 overlayProjection = Matrix4.Identity;
        private Matrix4 overlayModelview = Matrix4.Identity;

        private Matrix4 terrainProjection = Matrix4.Identity;
        private Matrix4 terrainModelview = Matrix4.Identity;

        private Matrix4 gbufferCombineProjection = Matrix4.Identity;
        private Matrix4 gbufferCombineModelview = Matrix4.Identity;

        private Font font = new Font();
        private TextManager textManager = new TextManager("DefaultText", null);

        private FrameCounter frameCounter = new FrameCounter();
        private TextBlock frameCounterText = new TextBlock("fps", "", new Vector3(0.01f, 0.05f, 0.0f), 0.0005f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        private PerfMonitor perfmon = new PerfMonitor();

        private Vector3 eyePos;

        private double globalTime = 0.0;
        private double angle = 0.0;
        private double viewHeight = 100.0;

        private GBuffer gbuffer = new GBuffer("gbuffer1");
        private GBufferCombiner gbufferCombiner;


        private TerrainTile tile = new TerrainTile(TILESIZE, TILESIZE);
        //private ITileRenderer renderer = new BoundingBoxRenderer();
        //private ITileRenderer renderer = new MeshRenderer(64,64);
        private List<IGameComponent> renderers = new List<IGameComponent>();
        private int currentRenderer = 0;


        // key mappings
        private Dictionary<Key, Action> keyDownActions = new Dictionary<Key, Action>();



        public class CloseEventArgs : EventArgs { }
        public delegate void CloseEventHandler(object source, CloseEventArgs e);
        public event CloseEventHandler OnClose;

        public TerrainViewer()
            : base(640, 480, new GraphicsMode(), "Snowscape", GameWindowFlags.Default, DisplayDevice.Default, 3, 1, GraphicsContextFlags.Default)
        {
            this.Load += new EventHandler<EventArgs>(TerrainViewer_Load);
            this.Closed += new EventHandler<EventArgs>(TerrainViewer_Closed);
            this.UpdateFrame += new EventHandler<FrameEventArgs>(TerrainViewer_UpdateFrame);
            this.RenderFrame += new EventHandler<FrameEventArgs>(TerrainViewer_RenderFrame);
            this.Resize += new EventHandler<EventArgs>(TerrainViewer_Resize);
            this.Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(Keyboard_KeyDown);
        }


        void TerrainViewer_Resize(object sender, EventArgs e)
        {
            SetProjection();
            this.gbuffer.Init(this.ClientRectangle.Width, this.ClientRectangle.Height);
        }

        private void SetProjection()
        {
            GL.Viewport(this.ClientRectangle);
            SetOverlayProjection();
            SetTerrainProjection();
            SetGBufferCombineProjection();
        }

        private void SetOverlayProjection()
        {
            this.overlayProjection = Matrix4.CreateOrthographicOffCenter(0.0f, (float)this.ClientRectangle.Width / (float)this.ClientRectangle.Height, 1.0f, 0.0f, 0.001f, 10.0f);
            this.overlayModelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);
        }

        private void SetGBufferCombineProjection()
        {
            this.gbufferCombineProjection = Matrix4.CreateOrthographicOffCenter(0.0f, 1.0f, 1.0f, 0.0f, 0.001f, 10.0f);
            this.gbufferCombineModelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);
        }

        private void SetTerrainProjection()
        {
            this.terrainProjection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI * 0.4f, (float)this.ClientRectangle.Width / (float)this.ClientRectangle.Height, 0.1f, 4000.0f);

            double r = 200.0f;
            double a = angle; // Math.IEEERemainder(globalTime * 0.02, 1.0) * 2.0 * Math.PI;
            this.eyePos = new Vector3((float)(128.0 + r * Math.Cos(a)), (float)viewHeight, (float)(128.0 + r * Math.Sin(a)));

            this.terrainModelview = Matrix4.LookAt(this.eyePos, new Vector3(128.0f, 0.0f, 128.0f), -Vector3.UnitY);
        }

        private void SetupGBufferCombiner()
        {

            var program = new ShaderProgram("combiner");

            program.Init(
                @"GBufferCombine.vert",
                @"GBufferCombine.frag",
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "in_texcoord0") 
                });

            this.gbufferCombiner = new GBufferCombiner(this.gbuffer, program);

        }

        private void RenderGBufferCombiner(Matrix4 projection, Matrix4 modelview)
        {

            this.gbufferCombiner.Render(projection, modelview, (sp) =>
            {
                sp
                    .SetUniform("eyePos", this.eyePos)
                    .SetUniform("sunVector", Vector3.Normalize(new Vector3(0.2f, 0.8f, 0.3f)))
                    .SetUniform("posTex", 0)
                    .SetUniform("normalTex", 1)
                    .SetUniform("shadeTex", 2)
                    .SetUniform("paramTex", 3);
            });
        }

        void TerrainViewer_Load(object sender, EventArgs e)
        {
            this.VSync = VSyncMode.Off;

            // GL state
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(new Color4(0, 24, 64, 255));

            // setup font
            font.Init(Resources.FontConsolas, Resources.FontConsolasMeta);
            textManager.Font = font;

            this.tile.Load();
            this.tile.SetupTestData();

            this.tile.ModelMatrix = Matrix4.CreateTranslation(16f, 0f, 0f);

            //this.renderers.Add(new BoundingBoxRenderer());
            this.renderers.Add(new MeshRenderer(TILESIZE, TILESIZE));
            //this.renderers.Add(new GenerationVisMeshRenderer(TILESIZE, TILESIZE));
            this.renderers.Add(new RaycastRenderer());

            foreach (var renderer in renderers)
            {
                renderer.Load();
            }

            
            this.gbuffer.SetSlot(0, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // pos
            this.gbuffer.SetSlot(1, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // normal
            this.gbuffer.SetSlot(2, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // shade
            this.gbuffer.SetSlot(3, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // param
            this.gbuffer.Init(this.ClientRectangle.Width, this.ClientRectangle.Height);

            this.SetupGBufferCombiner();


            this.SetKeyMappings();

            this.frameCounter.Start();
        }

        private void SetKeyMappings()
        {
            AddKeyMapping(Key.R, () =>
            {
                currentRenderer++;
                currentRenderer %= this.renderers.Count;
            });

            AddKeyMapping(Key.Escape, () => { this.Close(); });

            //AddKeyMapping(Key.Z, () => { this.angle += 0.05; });
            //AddKeyMapping(Key.X, () => { this.angle -= 0.05; });
        }

        void TerrainViewer_UpdateFrame(object sender, FrameEventArgs e)
        {
            //this.camera.Update(e.Time);

            globalTime += e.Time;

            if (Keyboard[Key.Z] || Keyboard[Key.Left])
            {
                this.angle += e.Time * 0.5;
            }
            if (Keyboard[Key.X] || Keyboard[Key.Right])
            {
                this.angle -= e.Time * 0.5;
            }
            if (Keyboard[Key.Up]) { this.viewHeight += 1.0; }
            if (Keyboard[Key.Down]) { this.viewHeight -= 1.0; }

        }

        void AddKeyMapping(Key key, Action action)
        {
            if (!this.keyDownActions.ContainsKey(key))
            {
                this.keyDownActions.Add(key, action);
            }
            else
            {
                this.keyDownActions[key] = action;
            }
        }


        void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            Action action;

            if (this.keyDownActions.TryGetValue(e.Key, out action))
            {
                action();
            }
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

            SetTerrainProjection();


            this.gbuffer.BindForWriting();

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.Blend);

            perfmon.Start("RenderBox");
            this.renderers[currentRenderer].Render(tile, this.terrainProjection, this.terrainModelview, this.eyePos);
            perfmon.Stop("RenderBox");

            this.gbuffer.UnbindFromWriting();


            GL.Viewport(this.ClientRectangle);
            GL.ClearColor(0.0f, 0.0f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            perfmon.Start("RenderGBufferCombiner");
            RenderGBufferCombiner(gbufferCombineProjection, gbufferCombineModelview);
            perfmon.Stop("RenderGBufferCombiner");

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


        void TerrainViewer_Closed(object sender, EventArgs e)
        {
            OnClose(this, new CloseEventArgs());
        }



    }
}
