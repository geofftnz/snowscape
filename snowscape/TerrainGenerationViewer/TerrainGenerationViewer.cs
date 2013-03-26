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
        private Sampler heightTexSampler = new Sampler("GBheightTexSampler");
        private TerrainTile terrainTile;
        private ITileRenderer tileRenderer;
        private ITileRenderer tileRendererRaycast;
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

        private uint updateThreadIterations;
        private uint prevThreadIterations;
        private double updateThreadUpdateTime = 0.0;
        private long waterIterations = 0;
        private int textureUpdateCount = 0;

        private PerfMonitor perfmon = new PerfMonitor();

        private string terrainPath = @"../../../../terrains/";

        private Vector2 view_offset = Vector2.Zero;
        private float view_scale = 1.0f;


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
            : base(640, 480, new GraphicsMode(), "Snowscape", GameWindowFlags.Default, DisplayDevice.Default, 3, 3, GraphicsContextFlags.Default)
        {
            this.Terrain = new TerrainGen(TileWidth, TileHeight);
            this.terrainTile = new TerrainTile(TileWidth, TileHeight);
            this.tileRenderer = new GenerationVisMeshRenderer(TileWidth, TileHeight);
            this.tileRendererRaycast = new GenerationVisRaycastRenderer();

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
            this.tileRenderer.Load();
            this.tileRendererRaycast.Load();

            this.gbuffer.SetSlot(0, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // pos
            //this.gbuffer.SetSlot(1, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // normal
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

            this.heightTexSampler.Init();
            this.heightTexSampler
                .SetParameter(new SamplerObjectParameterInt(SamplerParameter.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new SamplerObjectParameterInt(SamplerParameter.TextureMinFilter, (int)TextureMinFilter.Linear))
                .SetParameter(new SamplerObjectParameterInt(SamplerParameter.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new SamplerObjectParameterInt(SamplerParameter.TextureWrapT, (int)TextureWrapMode.Repeat))
                .ApplyParameters();


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
            /*
            this.terrainProjection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI * 0.4f, (float)this.ClientRectangle.Width / (float)this.ClientRectangle.Height, 0.1f, 4000.0f);

            double r = 200.0f;
            double a = angle; // Math.IEEERemainder(globalTime * 0.02, 1.0) * 2.0 * Math.PI;
            this.eyePos = new Vector3((float)(128.0 + r * Math.Cos(a)), (float)viewHeight, (float)(128.0 + r * Math.Sin(a)));

            this.terrainModelview = Matrix4.LookAt(this.eyePos, new Vector3(128.0f, 0.0f, 128.0f), -Vector3.UnitY);
             */
        }



        void TerrainGenerationViewer_UpdateFrame(object sender, FrameEventArgs e)
        {
            /*
            float moveRate = 0.02f * this.view_scale;
            if (Keyboard[Key.Plus])
            {
                this.view_scale *= 0.95f;
            }

            if (Keyboard[Key.Minus])
            {
                this.view_scale *= 1.05f;
            }
            
            if (Keyboard[Key.Left])
            {
                this.view_offset.X -= moveRate;
            }
            if (Keyboard[Key.Right])
            {
                this.view_offset.X += moveRate;
            }
            if (Keyboard[Key.Up])
            {
                this.view_offset.Y -= moveRate;
            }
            if (Keyboard[Key.Down])
            {
                this.view_offset.Y += moveRate;
            }

            this.view_offset.X = this.view_offset.X.Wrap(1.0f);
            this.view_offset.Y = this.view_offset.Y.Wrap(1.0f);
            */

            var pos = (this.camera as WalkCamera).Position;
            pos.Y = this.Terrain.Terrain.HeightAt(pos.X, pos.Z);
            (this.camera as WalkCamera).Position = pos;

            this.camera.Update(e.Time);
            this.eyePos = (this.camera as WalkCamera).EyePos;

            if (Keyboard[Key.M])
            {
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(Location.X + Width / 2, Location.Y + Height / 2);
            }

            if (Keyboard[Key.Z] || Keyboard[Key.Left])
            {
                this.angle += e.Time * 0.5;
            }
            if (Keyboard[Key.X] || Keyboard[Key.Right])
            {
                this.angle -= e.Time * 0.5;
            }
            if (Keyboard[Key.Up]) { this.viewHeight += 10.0; }
            if (Keyboard[Key.Down]) { this.viewHeight -= 10.0; }


            if (Keyboard[Key.R])
            {
                if (updateCounter > 10)
                {
                    ResetTerrain();
                    ResetCounters();
                }
            }


            updateCounter++;
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
            this.terrainTile.HeightTexture.Bind(TextureUnit.Texture2);
            this.heightTexSampler.Bind(TextureUnit.Texture2);

            this.gbufferCombiner.Render(projection, modelview, (sp) =>
            {
                sp.SetUniform("eyePos", this.eyePos);
                //.SetUniform("sunVector", Vector3.Normalize(new Vector3(0.2f, 0.8f, 0.3f)))
                sp.SetUniform("posTex", 0);
                //sp.SetUniform("normalTex", 1);
                sp.SetUniform("paramTex", 1);
                sp.SetUniform("heightTex", 2);
                sp.SetUniform("boxparam",new Vector4((float)this.terrainTile.Width, (float)this.terrainTile.Height, 0.0f, 1.0f));
            });

            Sampler.Unbind(TextureUnit.Texture2);
        }


        void TerrainGenerationViewer_RenderFrame(object sender, FrameEventArgs e)
        {

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
                textureUpdateCount++;
                prevThreadIterations = currentThreadIterations;
            }

            SetTerrainProjection();

            // render terrain to gbuffer
            perfmon.Start("RenderTerrain");
            this.gbuffer.BindForWriting();
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.Blend);
            GL.ColorMask(true, true, true, true);

            RenderTiles();
            this.gbuffer.UnbindFromWriting();
            perfmon.Stop("RenderTerrain");

            // render gbuffer to screen

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
