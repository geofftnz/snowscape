using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;
using Utils;
using TerrainGeneration;
using System.Threading;
using System.IO;
using NLog;
using System.Diagnostics;
using OpenTK.Input;
using Snowscape.TerrainStorage;
using Snowscape.TerrainRenderer;
using Snowscape.TerrainRenderer.Renderers;
using OpenTKExtensions.Camera;
using Atmosphere = Snowscape.TerrainRenderer.Atmosphere;


namespace Snowscape.TerrainGenerationViewer
{
    public class TerrainGenerationViewer : GameWindow
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        const int TileWidth = 1024;
        const int TileHeight = 1024;

        public TerrainGen Terrain { get; set; }

        private Matrix4 overlayProjection = Matrix4.Identity;
        private Matrix4 overlayModelview = Matrix4.Identity;

        private Matrix4 terrainProjection = Matrix4.Identity;
        private Matrix4 terrainModelview = Matrix4.Identity;

        private Matrix4 gbufferCombineProjection = Matrix4.Identity;
        private Matrix4 gbufferCombineModelview = Matrix4.Identity;


        private GBuffer gbuffer = new GBuffer("gb");
        private GBufferCombiner gbufferCombiner;
        private TerrainTile terrainTile;
        private TerrainGlobal terrainGlobal;
        private ITileRenderer tileRenderer;
        private ITileRenderer tileRendererRaycast;
        private Atmosphere.RayDirectionRenderer skyRenderer = new Atmosphere.RayDirectionRenderer();
        private TerrainLightingGenerator terrainLighting;

        private float sunElevation = 0.2f;
        private float sunAzimuth = 0.3f;
        private Vector3 sunDirection = Vector3.Normalize(new Vector3(0.8f, 0.15f, 0.6f));
        private Vector3 prevSunDirection = Vector3.Zero;

        private Vector3 eyePos;
        private double angle = 0.0;
        private double viewHeight = 100.0;

        private ICamera camera;


        uint updateCounter = 0;
        private Font font = new Font();
        private TextManager textManager = new TextManager("DefaultText", null);

        private FrameCounter frameCounter = new FrameCounter();
        private TextBlock frameCounterText = new TextBlock("fps", "", new Vector3(0.01f, 0.05f, 0.0f), 0.0005f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

        private Thread updateThread;
        private bool killThread = false;
        private bool pauseThread = false;
        private bool threadPaused = false;

        private bool KillThread
        {
            get
            {
                bool temp;
                lock (this)
                {
                    temp = this.killThread;
                }
                return temp;
            }
            set
            {
                lock (this)
                {
                    this.killThread = value;
                }
            }
        }
        private bool PauseThread
        {
            get
            {
                bool temp;
                lock (this)
                {
                    temp = this.pauseThread;
                }
                return temp;
            }
            set
            {
                lock (this)
                {
                    this.pauseThread = value;
                }
            }
        }
        private bool ThreadPaused
        {
            get
            {
                bool temp;
                lock (this)
                {
                    temp = this.threadPaused;
                }
                return temp;
            }
            set
            {
                lock (this)
                {
                    this.threadPaused = value;
                }
            }
        }

        private Terrain threadCopyMap;
        private Terrain threadRenderMap;

        private bool pauseUpdate = false;

        private uint updateThreadIterations;
        private uint prevThreadIterations;
        private double updateThreadUpdateTime = 0.0;
        private long waterIterations = 0;
        private int textureUpdateCount = 0;

        private PerfMonitor perfmon = new PerfMonitor();

        private string terrainPath = @"../../../../terrains/";

        private Vector2 view_offset = Vector2.Zero;


        private Vector3[] quadPos = new Vector3[]{
            new Vector3(0f,0f,0f),
            new Vector3(0f,1f,0f),
            new Vector3(1f,0f,0f),
            new Vector3(1f,1f,0f)
        };

        private Vector2[] quadTexCoord = new Vector2[]{
            new Vector2(0f,0f),
            new Vector2(0f,1f),
            new Vector2(1f,0f),
            new Vector2(1f,1f)
        };

        private uint[] quadIndex = new uint[] { 0, 1, 2, 3 };

        public class CloseEventArgs : EventArgs { }
        public delegate void CloseEventHandler(object source, CloseEventArgs e);
        public event CloseEventHandler OnClose;

        public TerrainGenerationViewer()
            : base(640, 480, new GraphicsMode(), "Snowscape", GameWindowFlags.Default, DisplayDevice.Default, 3, 1, GraphicsContextFlags.Default)
        {
            this.Terrain = new TerrainGen(TileWidth, TileHeight);
            this.terrainTile = new TerrainTile(TileWidth, TileHeight);
            this.terrainGlobal = new TerrainGlobal(TileWidth, TileHeight);
            this.tileRenderer = new GenerationVisMeshRenderer(TileWidth, TileHeight);
            this.tileRendererRaycast = new GenerationVisRaycastRenderer();
            this.terrainLighting = new TerrainLightingGenerator(TileWidth, TileHeight);

            this.camera = new WalkCamera(this.Keyboard, this.Mouse);

            this.UpdateFrame += new EventHandler<FrameEventArgs>(TerrainGenerationViewer_UpdateFrame);
            this.RenderFrame += new EventHandler<FrameEventArgs>(TerrainGenerationViewer_RenderFrame);
            this.Load += new EventHandler<EventArgs>(TerrainGenerationViewer_Load);
            this.Unload += new EventHandler<EventArgs>(TerrainGenerationViewer_Unload);
            this.Resize += new EventHandler<EventArgs>(TerrainGenerationViewer_Resize);
            this.Closed += new EventHandler<EventArgs>(TerrainGenerationViewer_Closed);
            this.Closing += new EventHandler<System.ComponentModel.CancelEventArgs>(TerrainGenerationViewer_Closing);

            this.Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(Keyboard_KeyDown);

        }

        void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            if (e.Key == Key.R)
            {
                ResetTerrain();
                ResetCounters();
            }
            if (e.Key == Key.P)
            {
                this.perfmon.ResetAll();
            }
            if (e.Key == Key.Up) { this.sunElevation *= 1.05f; this.CalculateSunDirection(); }
            if (e.Key == Key.Down) { this.sunElevation *= 0.95f; this.CalculateSunDirection(); }
            if (e.Key == Key.Left) { this.sunAzimuth += 0.01f; this.CalculateSunDirection(); }
            if (e.Key == Key.Right) { this.sunAzimuth -= 0.01f; this.CalculateSunDirection(); }

            if (e.Key == Key.Space)
            {
                if (pauseUpdate) // currently paused
                {
                    // resume thread
                    this.PauseThread = false;
                    this.pauseUpdate = false;
                }
                else // currently running
                {
                    log.Info("Pausing Update - pausing thread");
                    this.PauseThread = true;

                    while (!this.ThreadPaused)
                    {
                        Thread.Sleep(1);
                    }
                    log.Info("Pausing Update - thread paused");
                    this.pauseUpdate = true;

                }
            }
        }



        void TerrainGenerationViewer_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // kill thread
            log.Trace("Sending kill signal to update thread");
            lock (this)
            {
                this.killThread = true;
            }

            // wait for thread to complete
            log.Trace("Waiting for thread to complete...");
            this.updateThread.Join(2000);

            if (this.updateThread.IsAlive)
            {
                log.Warn("Thread.Join timeout - aborting");
                this.updateThread.Abort();
            }

            log.Trace("Saving terrain into slot 0...");
            this.Terrain.Save(this.GetTerrainFileName(0));
        }

        void TerrainGenerationViewer_Closed(object sender, EventArgs e)
        {
            OnClose(this, new CloseEventArgs());
        }


        void TerrainGenerationViewer_Load(object sender, EventArgs e)
        {
            this.VSync = VSyncMode.Off;

            // create VBOs/Shaders etc

            this.terrainTile.Init();
            this.terrainGlobal.Init();
            this.tileRenderer.Load();
            this.tileRendererRaycast.Load();
            this.terrainLighting.Init(this.terrainGlobal.ShadeTexture);

            this.gbuffer.SetSlot(0, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // pos
            this.gbuffer.SetSlot(1, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // param
            this.gbuffer.Init(this.ClientRectangle.Width, this.ClientRectangle.Height);

            var program = new ShaderProgram("combiner");

            program.Init(
                @"../../../Resources/Shaders/GBufferVisCombine.vert".Load(),
                @"../../../Resources/Shaders/GBufferVisCombine.frag".Load(),
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "in_texcoord0") 
                });

            this.gbufferCombiner = new GBufferCombiner(this.gbuffer, program);

            this.skyRenderer.Load();

            // GL state
            GL.Enable(EnableCap.DepthTest);

            // setup font
            font.Init(Resources.FontConsolas, Resources.FontConsolasMeta);
            textManager.Font = font;

            SetProjection();

            try
            {
                this.Terrain.Load(this.GetTerrainFileName(0));
            }
            catch (FileNotFoundException)
            {
                this.Terrain.InitTerrain1();
            }

            this.threadCopyMap = new Terrain(this.Terrain.Width, this.Terrain.Height);
            this.threadRenderMap = new Terrain(this.Terrain.Width, this.Terrain.Height);

            this.frameCounter.Start();

            log.Trace("Starting update thread...");
            this.updateThread = new Thread(new ThreadStart(this.UpdateThreadProc));
            this.updateThread.Start();

            this.CalculateSunDirection();
        }

        private void UpdateThreadProc()
        {
            //bool killMe = false;
            bool pauseMe = false;
            uint iteration = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            double startTime = 0.0;
            double updateTime = 0.0;


            while (true)
            {
                // check for kill request.
                if (this.KillThread)
                {
                    break;
                }

                bool pauseRequest = this.PauseThread;

                if (pauseMe != pauseRequest)
                {
                    pauseMe = pauseRequest;
                    this.ThreadPaused = pauseMe;
                }

                if (!pauseMe)
                {
                    startTime = sw.Elapsed.TotalMilliseconds;
                    this.Terrain.ModifyTerrain();
                    updateTime = sw.Elapsed.TotalMilliseconds - startTime;

                    iteration++;

                    if (iteration % 4 == 0)
                    {
                        lock (this)
                        {
                            this.threadCopyMap.CopyFrom(this.Terrain.Terrain);
                            this.updateThreadIterations = iteration;
                            this.updateThreadUpdateTime = this.updateThreadUpdateTime * 0.8 + 0.2 * updateTime;
                            this.waterIterations = this.Terrain.WaterIterations;
                        }
                    }
                }
                Thread.Sleep(1);
            }

        }

        protected void CopyMapDataFromUpdateThread()
        {
            lock (this)
            {
                perfmon.Start("Copy2");
                this.threadRenderMap.CopyFrom(this.threadCopyMap);
                perfmon.Stop("Copy2");
            }
        }


        void TerrainGenerationViewer_Unload(object sender, EventArgs e)
        {
        }

        void TerrainGenerationViewer_Resize(object sender, EventArgs e)
        {
            SetProjection();
            this.gbuffer.Init(this.ClientRectangle.Width, this.ClientRectangle.Height);
            this.camera.Resize(this.ClientRectangle.Width, this.ClientRectangle.Height);
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
            this.terrainProjection = this.camera.Projection;
            this.terrainModelview = this.camera.View;
        }



        void TerrainGenerationViewer_UpdateFrame(object sender, FrameEventArgs e)
        {

            var pos = (this.camera as WalkCamera).Position;
            pos.Y = this.Terrain.Terrain.HeightAt(pos.X, pos.Z);
            (this.camera as WalkCamera).Position = pos;

            this.camera.Update(e.Time);
            this.eyePos = (this.camera as WalkCamera).EyePos;


             updateCounter++;
        }

        private void CalculateSunDirection()
        {
            //if (this.sunElevation < 0.0f)
            this.sunElevation = this.sunElevation.Clamp(-0.2f, 1.0f);
            this.sunAzimuth = this.sunAzimuth.Wrap(1.0f);

            double phi = this.sunAzimuth * Math.PI * 2.0;
            double theta = (1.0 - this.sunElevation) * Math.PI * 0.5;
            double r = 1.0;

            this.sunDirection.X = (float)(r * Math.Sin(theta) * Math.Cos(phi));
            this.sunDirection.Y = (float)(r * Math.Cos(theta));
            this.sunDirection.Z = (float)(r * Math.Sin(theta) * Math.Sin(phi));

            this.sunDirection.Normalize();
        }

        private void ResetCounters()
        {
            this.updateCounter = 0;
        }

        private void ResetTerrain()
        {
            log.Info("Resetting Terrain - pausing thread");
            this.PauseThread = true;

            while (!this.ThreadPaused)
            {
                Thread.Sleep(1);
            }
            log.Info("Resetting Terrain - thread paused");

            this.Terrain.ResetTerrain();

            log.Info("Resetting Terrain - restarting thread");
            this.PauseThread = false;
        }


        private void RenderGBufferCombiner(Matrix4 projection, Matrix4 modelview)
        {
            this.terrainGlobal.HeightTexture.Bind(TextureUnit.Texture2);
            this.terrainGlobal.ShadeTexture.Bind(TextureUnit.Texture3);

            this.gbufferCombiner.Render(projection, modelview, (sp) =>
            {
                sp.SetUniform("eyePos", this.eyePos);
                sp.SetUniform("sunVector", this.sunDirection);
                sp.SetUniform("posTex", 0);
                sp.SetUniform("paramTex", 1);
                sp.SetUniform("heightTex", 2);
                sp.SetUniform("shadeTex", 3);
                sp.SetUniform("minHeight", this.terrainGlobal.MinHeight);
                sp.SetUniform("maxHeight", this.terrainGlobal.MaxHeight);
                sp.SetUniform("boxparam", new Vector4((float)this.terrainTile.Width, (float)this.terrainTile.Height, 0.0f, 1.0f));
            });

        }


        void TerrainGenerationViewer_RenderFrame(object sender, FrameEventArgs e)
        {
            bool needToRenderLighting = false;

            if (this.frameCounter.Frames % 32 == 0)
            {
                frameCounterText.Text = string.Format("FPS: {0:0} {1:###0} updates: {2:0.0}ms {3:#,###,###,##0} water iterations.", frameCounter.FPSSmooth, this.updateThreadIterations, this.updateThreadUpdateTime, this.waterIterations);
                textManager.AddOrUpdate(frameCounterText);

                float y = 0.1f;
                foreach (var timer in this.perfmon.AllAverageTimes())
                {
                    textManager.AddOrUpdate(new TextBlock("perf" + timer.Item1, string.Format("{0}: {1:0.000} ms", timer.Item1, timer.Item2), new Vector3(0.01f, y, 0.0f), 0.00025f, new Vector4(1.0f, 1.0f, 1.0f, 0.5f)));
                    y += 0.0125f;
                }
            }

            uint currentThreadIterations = updateThreadIterations;
            if (prevThreadIterations != currentThreadIterations)
            {
                CopyMapDataFromUpdateThread();
                this.terrainTile.SetDataFromTerrainGeneration(this.threadRenderMap, 0, 0);
                this.terrainGlobal.SetDataFromTerrain(this.threadRenderMap);
                textureUpdateCount++;
                prevThreadIterations = currentThreadIterations;

                needToRenderLighting = true;
            }

            if (prevSunDirection != sunDirection || needToRenderLighting)
            {
                // render lighting
                perfmon.Start("Lighting");
                this.RenderLighting(this.sunDirection);
                perfmon.Stop("Lighting");

                this.prevSunDirection = this.sunDirection;
            }

            SetTerrainProjection();

            // render terrain to gbuffer
            this.gbuffer.BindForWriting();
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.ClearDepth(1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.ColorMask(true, true, true, true);

            perfmon.Start("RenderTerrain");
            RenderTiles();
            perfmon.Stop("RenderTerrain");

            perfmon.Start("RenderSkyRays");
            RenderSkyRayDirections();
            perfmon.Stop("RenderSkyRays");
            this.gbuffer.UnbindFromWriting();


            // render gbuffer to screen

            GL.Viewport(this.ClientRectangle);
            GL.ClearColor(0.0f, 0.0f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            perfmon.Start("RenderGBufferCombiner");
            RenderGBufferCombiner(gbufferCombineProjection, gbufferCombineModelview);
            perfmon.Stop("RenderGBufferCombiner");


            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
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

        private void RenderLighting(Vector3 sunVector)
        {
            this.terrainLighting.Render(sunVector, this.terrainGlobal.HeightTexture, this.terrainGlobal.MinHeight, this.terrainGlobal.MaxHeight);
        }

        private void RenderSkyRayDirections()
        {
            this.skyRenderer.Render(this.terrainProjection, this.terrainModelview, this.eyePos);
        }

        private void RenderTiles()
        {
            //RenderTile(this.terrainTile, 0f, 0f, this.tileRendererRaycast);

            RenderTile(this.terrainTile, -1f, -1f, this.tileRendererRaycast);
            RenderTile(this.terrainTile, -1f, 0f, this.tileRendererRaycast);
            RenderTile(this.terrainTile, -1f, 1f, this.tileRendererRaycast);

            RenderTile(this.terrainTile, 0f, -1f, this.tileRendererRaycast);
            RenderTile(this.terrainTile, 0f, 0f, this.tileRenderer);
            RenderTile(this.terrainTile, 0f, 1f, this.tileRendererRaycast);

            RenderTile(this.terrainTile, 1f, -1f, this.tileRendererRaycast);
            RenderTile(this.terrainTile, 1f, 0f, this.tileRendererRaycast);
            RenderTile(this.terrainTile, 1f, 1f, this.tileRendererRaycast);
        }

        private void RenderTile(TerrainTile tile, float TileXOffset, float TileZOffset, ITileRenderer renderer)
        {
            tile.ModelMatrix = Matrix4.CreateTranslation(TileXOffset * (float)tile.Width, 0f, TileZOffset * (float)tile.Height);
            renderer.Render(tile, this.terrainProjection, this.terrainModelview, this.eyePos);
        }

        protected string GetTerrainFileName(int index)
        {
            return string.Format("{0}Terrain{1}.1024.pass1.ter", this.terrainPath, index);
        }

    }
}
