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
using Lighting = Snowscape.TerrainRenderer.Lighting;
using HDR = Snowscape.TerrainRenderer.HDR;


namespace Snowscape.TerrainGenerationViewer
{
    public class TerrainGenerationViewer : GameWindow
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        const int TileWidth = 1024;
        const int TileHeight = 1024;

        const int SkyRes = 512;
        const int CloudRes = 512;

        public TerrainGen Terrain { get; set; }

        private Matrix4 overlayProjection = Matrix4.Identity;
        private Matrix4 overlayModelview = Matrix4.Identity;

        private Matrix4 terrainProjection = Matrix4.Identity;
        private Matrix4 terrainModelview = Matrix4.Identity;

        //private Matrix4 gbufferCombineProjection = Matrix4.Identity;
        //private Matrix4 gbufferCombineModelview = Matrix4.Identity;


        //private GBuffer gbuffer = new GBuffer("gb");
        //private GBufferCombiner gbufferCombiner;

        private Lighting.LightingCombiner lightingStep = new Lighting.LightingCombiner();

        private TerrainTile terrainTile;
        private TerrainGlobal terrainGlobal;
        private ITileRenderer tileRenderer;
        private ITileRenderer tileRendererRaycast;
        private Atmosphere.RayDirectionRenderer skyRayDirectionRenderer = new Atmosphere.RayDirectionRenderer();
        private TerrainLightingGenerator terrainLighting;

        private HDR.HDRExposureMapper hdrExposure = new HDR.HDRExposureMapper();


        //private Texture skyTexture;
        private Atmosphere.SkyScatteringCubeRenderer skyRenderer = new Atmosphere.SkyScatteringCubeRenderer(SkyRes);

        private Texture skyCubeTexture;

        private Texture cloudTexture;
        private Texture cloudDepthTexture;
        private Atmosphere.CloudDepthRenderer cloudDepthRenderer = new Atmosphere.CloudDepthRenderer();
        //private Vector3 cloudScale = new Vector3(0.0001f,600.0f,0.0001f);
        private Vector3 cloudScale = new Vector3(0.0002f, 600.0f, 0.0002f);

        private Vector3 sunDirection = Vector3.Normalize(new Vector3(0.8f, 0.15f, 0.6f));
        private Vector3 prevSunDirection = Vector3.Zero;

        private ParameterCollection parameters = new ParameterCollection();

        private Vector3 eyePos;

        private ICamera camera;


        uint updateCounter = 0;
        private Font font = new Font();
        private TextManager textManager = new TextManager("DefaultText", null);

        private FrameCounter frameCounter = new FrameCounter();
        private TextBlock frameCounterText = new TextBlock("fps", "", new Vector3(0.01f, 0.05f, 0.0f), 0.0003f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

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
        private uint currentParamsVersion = 0;
        private uint prevParamsVersion = 0;

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

            //parameters.Add(new Parameter<float>("exposure", -1.0f, -100.0f, -0.0005f, v => v * 1.05f, v => v * 0.95f));
            parameters.Add(new Parameter<float>("TargetLuminance", 0.2f, 0.01f, 1.0f, v => v += 0.01f, v => v -= 0.01f));
            parameters.Add(new Parameter<float>("WhiteLevel", 4.0f, 0.05f, 100.0f, v => v += 0.05f, v => v -= 0.05f));
            parameters.Add(new Parameter<float>("sunElevation", 0.2f, -1.0f, 1.0f, v => v + 0.005f, v => v - 0.005f));
            parameters.Add(new Parameter<float>("sunAzimuth", 0.2f, 0.0f, 1.0f, v => v + 0.01f, v => v - 0.01f));

            //vec3(0.100,0.598,0.662) * 1.4
            //0.18867780436772762, 0.4978442963618773, 0.6616065586417131
            parameters.Add(new Parameter<float>("Kr_r", 0.1287f, 0.0f, 1.0f, v => v + 0.002f, v => v - 0.002f));
            parameters.Add(new Parameter<float>("Kr_g", 0.1898f, 0.0f, 1.0f, v => v + 0.002f, v => v - 0.002f));
            parameters.Add(new Parameter<float>("Kr_b", 0.6616f, 0.0f, 1.0f, v => v + 0.002f, v => v - 0.002f));  // 0.6616
            parameters.Add(new Parameter<float>("Sun_r", 5.0f, 0.0f, 16.0f, v => v + 0.02f, v => v - 0.02f));
            parameters.Add(new Parameter<float>("Sun_g", 4.4f, 0.0f, 16.0f, v => v + 0.02f, v => v - 0.02f));
            parameters.Add(new Parameter<float>("Sun_b", 4.0f, 0.0f, 16.0f, v => v + 0.02f, v => v - 0.02f));  // 0.6616

            parameters.Add(new Parameter<float>("scatterAbsorb", 0.15f, 0.0001f, 4.0f, v => v * 1.02f, v => v * 0.98f));  // 0.028  0.1

            parameters.Add(new Parameter<float>("mieBrightness", 0.005f, 0.0001f, 40.0f, v => v * 1.02f, v => v * 0.98f));
            parameters.Add(new Parameter<float>("raleighBrightness", 0.2f, 0.0001f, 40.0f, v => v * 1.02f, v => v * 0.98f));
            parameters.Add(new Parameter<float>("skylightBrightness", 3.0f, 0.0001f, 40.0f, v => v * 1.02f, v => v * 0.98f));

            parameters.Add(new Parameter<float>("sampleDistanceFactor", 0.01f, 0.0000001f, 1.0f, v => v * 1.05f, v => v * 0.95f));

            parameters.Add(new Parameter<float>("groundLevel", 0.996f, 0.5f, 0.99999f, v => v + 0.0001f, v => v - 0.0001f)); // 0.995 0.98

            parameters.Add(new Parameter<float>("cloudLevel", 100.0f, -1000.0f, 1000.0f, v => v + 50f, v => v - 50f));
            parameters.Add(new Parameter<float>("cloudThickness", 500.0f, 10.0f, 2000.0f, v => v + 10f, v => v - 10f));

            parameters.Add(new Parameter<float>("NearScatterDistance", 1200.0f, 10.0f, 20000.0f, v => v + 10f, v => v - 10f));
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

            if (e.Key == Key.Up)
            {
                this.parameters.CurrentIndex--;
            }
            if (e.Key == Key.Down)
            {
                this.parameters.CurrentIndex++;
            }

            if (e.Key == Key.Left)
            {
                this.parameters.Current.Decrease();
                currentParamsVersion++;
            }
            if (e.Key == Key.Right)
            {
                this.parameters.Current.Increase();
                currentParamsVersion++;
            }
            if (e.Key == Key.PageUp)
            {
                for (int i = 0; i < 10; i++)
                {
                    this.parameters.Current.Increase();
                }
                currentParamsVersion++;
            }
            if (e.Key == Key.PageDown)
            {
                for (int i = 0; i < 10; i++)
                {
                    this.parameters.Current.Decrease();
                }
                currentParamsVersion++;
            }




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

            //this.gbuffer.SetSlot(0, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // pos
            //this.gbuffer.SetSlot(1, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // param
            //this.gbuffer.Init(this.ClientRectangle.Width, this.ClientRectangle.Height);

            //var program = new ShaderProgram("combiner");

            //program.Init(
            //    @"../../../Resources/Shaders/GBufferVisCombine.vert".Load(),
            //    @"../../../Resources/Shaders/GBufferVisCombine.frag".Load(),
            //    new List<Variable> 
            //    { 
            //        new Variable(0, "vertex"), 
            //        new Variable(1, "in_texcoord0") 
            //    });

            //this.gbufferCombiner = new GBufferCombiner(this.gbuffer, program);

            this.lightingStep.Init(this.ClientRectangle.Width, this.ClientRectangle.Height);
            this.hdrExposure.Init(this.ClientRectangle.Width, this.ClientRectangle.Height);

            this.skyRayDirectionRenderer.Load();

            /*
            this.skyTexture = new Texture(SkyRes, SkyRes, TextureTarget.Texture2D, PixelInternalFormat.Rgb16f, PixelFormat.Rgb, PixelType.HalfFloat);
            this.skyTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat));
            this.skyTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));
            this.skyTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
            this.skyTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear));
            this.skyTexture.UploadEmpty();
            */

            this.skyRenderer.Init();

            GL.Enable(EnableCap.TextureCubeMap);
            GL.Enable(EnableCap.TextureCubeMapSeamless);
            this.skyCubeTexture = new Texture(SkyRes, SkyRes, TextureTarget.TextureCubeMap, PixelInternalFormat.Rgb16f, PixelFormat.Rgb, PixelType.HalfFloat);
            this.skyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge));
            this.skyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge));
            this.skyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge));
            this.skyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
            this.skyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapNearest));

            this.SetupCubeMap();


            // create noise texture for clouds
            this.cloudTexture = new NoiseTextureFactory(CloudRes, CloudRes).GenerateFloatTexture();
            // generate texture for cloud depth
            this.cloudDepthTexture = new Texture(CloudRes, CloudRes, TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);
            this.cloudDepthTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat));
            this.cloudDepthTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));
            this.cloudDepthTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
            this.cloudDepthTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear));
            this.cloudDepthTexture.UploadEmpty();
            // init cloud depth renderer
            this.cloudDepthRenderer.Init(this.cloudDepthTexture);

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

        private void SetupCubeMap()
        {
            this.skyCubeTexture.Bind();
            this.skyCubeTexture.ApplyParameters();
            this.skyCubeTexture.UploadEmpty(TextureTarget.TextureCubeMapNegativeX);
            this.skyCubeTexture.UploadEmpty(TextureTarget.TextureCubeMapNegativeY);
            this.skyCubeTexture.UploadEmpty(TextureTarget.TextureCubeMapNegativeZ);
            this.skyCubeTexture.UploadEmpty(TextureTarget.TextureCubeMapPositiveX);
            this.skyCubeTexture.UploadEmpty(TextureTarget.TextureCubeMapPositiveY);
            this.skyCubeTexture.UploadEmpty(TextureTarget.TextureCubeMapPositiveZ);
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
            //this.gbuffer.Init(this.ClientRectangle.Width, this.ClientRectangle.Height);
            this.lightingStep.Resize(this.ClientRectangle.Width, this.ClientRectangle.Height);
            this.hdrExposure.Resize(this.ClientRectangle.Width, this.ClientRectangle.Height);
            this.camera.Resize(this.ClientRectangle.Width, this.ClientRectangle.Height);
        }

        private void SetProjection()
        {
            GL.Viewport(this.ClientRectangle);
            SetOverlayProjection();
            SetTerrainProjection();
            //SetGBufferCombineProjection();
        }

        private void SetOverlayProjection()
        {
            this.overlayProjection = Matrix4.CreateOrthographicOffCenter(0.0f, (float)this.ClientRectangle.Width / (float)this.ClientRectangle.Height, 1.0f, 0.0f, 0.001f, 10.0f);
            this.overlayModelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);
        }

        //private void SetGBufferCombineProjection()
        //{
        //    this.gbufferCombineProjection = Matrix4.CreateOrthographicOffCenter(0.0f, 1.0f, 1.0f, 0.0f, 0.001f, 10.0f);
        //    this.gbufferCombineModelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);
        //}

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
            float sunElevation = (float)this.parameters["sunElevation"].GetValue();
            float sunAzimuth = (float)this.parameters["sunAzimuth"].GetValue();
            //if (this.sunElevation < 0.0f)
            sunElevation = sunElevation.Clamp(-0.5f, 1.0f);
            sunAzimuth = sunAzimuth.Wrap(1.0f);

            double phi = sunAzimuth * Math.PI * 2.0;
            double theta = (1.0 - sunElevation) * Math.PI * 0.5;
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


        private void RenderGBufferCombiner()
        {

            var rp = new Lighting.LightingCombiner.RenderParams()
            {
                HeightTexture = this.terrainGlobal.HeightTexture,
                ShadeTexture = this.terrainGlobal.ShadeTexture,
                CloudTexture = this.cloudTexture,
                CloudDepthTexture = this.cloudDepthTexture,
                SkyCubeTexture = this.skyCubeTexture,
                EyePos = this.eyePos,
                SunDirection = this.sunDirection,
                MinHeight = this.terrainGlobal.MinHeight,
                MaxHeight = this.terrainGlobal.MaxHeight,
                CloudScale = this.cloudScale,
                //Exposure = (float)this.parameters["exposure"].GetValue(),
                Kr = new Vector3(
                        (float)this.parameters["Kr_r"].GetValue(),
                        (float)this.parameters["Kr_g"].GetValue(),
                        (float)this.parameters["Kr_b"].GetValue()
                    ),
                SunLight = new Vector3(
                        (float)this.parameters["Sun_r"].GetValue(),
                        (float)this.parameters["Sun_g"].GetValue(),
                        (float)this.parameters["Sun_b"].GetValue()
                    ),
                ScatterAbsorb = (float)this.parameters["scatterAbsorb"].GetValue(),
                MieBrightness = (float)this.parameters["mieBrightness"].GetValue(),
                RaleighBrightness = (float)this.parameters["raleighBrightness"].GetValue(),
                SkylightBrightness = (float)this.parameters["skylightBrightness"].GetValue(),
                GroundLevel = (float)this.parameters["groundLevel"].GetValue(),
                CloudLevel = (float)this.parameters["cloudLevel"].GetValue(),
                CloudThickness = (float)this.parameters["cloudThickness"].GetValue(),
                TileWidth = this.terrainTile.Width,
                TileHeight = this.terrainTile.Height,
                SampleDistanceFactor = (float)this.parameters["sampleDistanceFactor"].GetValue(),
                NearScatterDistance = (float)this.parameters["NearScatterDistance"].GetValue()
            };

            this.lightingStep.Render(rp);


            //this.terrainGlobal.HeightTexture.Bind(TextureUnit.Texture2);
            //this.terrainGlobal.ShadeTexture.Bind(TextureUnit.Texture3);
            //this.cloudTexture.Bind(TextureUnit.Texture4);
            //this.cloudDepthTexture.Bind(TextureUnit.Texture5);
            ////this.skyTexture.Bind(TextureUnit.Texture7);
            //this.skyCubeTexture.Bind(TextureUnit.Texture6);

            //this.gbufferCombiner.Render(projection, modelview, (sp) =>
            //{
            //    sp.SetUniform("eyePos", this.eyePos);
            //    sp.SetUniform("sunVector", this.sunDirection);
            //    sp.SetUniform("posTex", 0);
            //    sp.SetUniform("paramTex", 1);
            //    sp.SetUniform("heightTex", 2);
            //    sp.SetUniform("shadeTex", 3);
            //    sp.SetUniform("noiseTex", 4);
            //    sp.SetUniform("cloudDepthTex", 5);
            //    //sp.SetUniform("skyTex", 7);
            //    sp.SetUniform("skyCubeTex", 6); 
            //    sp.SetUniform("minHeight", this.terrainGlobal.MinHeight);
            //    sp.SetUniform("maxHeight", this.terrainGlobal.MaxHeight);
            //    sp.SetUniform("cloudScale", this.cloudScale);
            //    sp.SetUniform("exposure", (float)this.parameters["exposure"].GetValue());
            //    sp.SetUniform("Kr",
            //        new Vector3(
            //            (float)this.parameters["Kr_r"].GetValue(),
            //            (float)this.parameters["Kr_g"].GetValue(),
            //            (float)this.parameters["Kr_b"].GetValue()
            //        ));
            //    sp.SetUniform("scatterAbsorb", (float)this.parameters["scatterAbsorb"].GetValue());
            //    sp.SetUniform("mieBrightness", (float)this.parameters["mieBrightness"].GetValue());
            //    sp.SetUniform("raleighBrightness", (float)this.parameters["raleighBrightness"].GetValue());
            //    sp.SetUniform("groundLevel", (float)this.parameters["groundLevel"].GetValue());
            //    sp.SetUniform("cloudLevel", (float)this.parameters["cloudLevel"].GetValue());
            //    sp.SetUniform("cloudThickness", (float)this.parameters["cloudThickness"].GetValue());


            //    sp.SetUniform("boxparam", new Vector4((float)this.terrainTile.Width, (float)this.terrainTile.Height, 0.0f, 1.0f));
            //});

        }


        void TerrainGenerationViewer_RenderFrame(object sender, FrameEventArgs e)
        {
            bool needToRenderLighting = false;

            //if (this.frameCounter.Frames % 32 == 0)
            //{
            frameCounterText.Text = string.Format("FPS: {0:0} Upd:{1:###0} {2:0.0}ms Water:{3:#,###,###,##0}", frameCounter.FPSSmooth, this.updateThreadIterations, this.updateThreadUpdateTime, this.waterIterations);
            textManager.AddOrUpdate(frameCounterText);

            float y = 0.1f;
            foreach (var timer in this.perfmon.AllAverageTimes())
            {
                textManager.AddOrUpdate(new TextBlock("perf" + timer.Item1, string.Format("{0}: {1:0.000} ms", timer.Item1, timer.Item2), new Vector3(0.01f, y, 0.0f), 0.00025f, new Vector4(1.0f, 1.0f, 1.0f, 0.5f)));
                y += 0.0125f;
            }

            y += 0.02f;
            for (int i = 0; i < this.parameters.Count; i++)
            {
                textManager.AddOrUpdate(
                    new TextBlock(
                        "param_" + this.parameters[i].Name,
                        this.parameters[i].ToString(),
                        new Vector3(0.01f, y, 0.0f),
                        0.0004f,
                        new Vector4(1.0f, 1.0f, 1.0f, (i == this.parameters.CurrentIndex) ? 1.0f : 0.5f)
                        )
                    );
                y += 0.025f;
            }

            y += 0.02f;
            textManager.AddOrUpdate(
                new TextBlock(
                    "eye",
                    this.eyePos.ToString(),
                    new Vector3(0.01f, y, 0.0f),
                    0.0004f,
                    new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                    )
                );

            //y += 0.02f;
            //textManager.AddOrUpdate(
            //    new TextBlock(
            //        "hdrdebug",
            //        this.hdrExposure.debugCol.ToString(),
            //        new Vector3(0.01f, y, 0.0f),
            //        0.0004f,
            //        new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
            //        )
            //    );

            //}

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

            this.CalculateSunDirection();
            if (prevSunDirection != sunDirection || prevParamsVersion != currentParamsVersion || needToRenderLighting)
            {
                // render lighting
                perfmon.Start("Lighting");
                this.RenderLighting(this.sunDirection);
                perfmon.Stop("Lighting");

                perfmon.Start("CloudDepth");
                this.RenderCloudDepth(this.sunDirection);
                perfmon.Stop("CloudDepth");

                perfmon.Start("SkyPreCalc");
                this.RenderSky(this.eyePos, this.sunDirection, (float)this.parameters["groundLevel"].GetValue());
                perfmon.Stop("SkyPreCalc");


                this.prevSunDirection = this.sunDirection;
                this.prevParamsVersion = this.currentParamsVersion;
            }

            SetTerrainProjection();

            // TODO: replace with call to lighting combiner
            // render terrain to gbuffer
            //this.gbuffer.BindForWriting();
            //GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            //GL.ClearDepth(1.0f);
            //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            //GL.Disable(EnableCap.Blend);
            //GL.Enable(EnableCap.DepthTest);
            //GL.ColorMask(true, true, true, true);
            this.lightingStep.BindForWriting();

            perfmon.Start("RenderTerrain");
            RenderTiles();
            perfmon.Stop("RenderTerrain");

            perfmon.Start("RenderSkyRays");
            RenderSkyRayDirections();
            perfmon.Stop("RenderSkyRays");

            //this.gbuffer.UnbindFromWriting();
            this.lightingStep.UnbindFromWriting();


            this.hdrExposure.TargetLuminance = (float)this.parameters["TargetLuminance"].GetValue();
            this.hdrExposure.WhiteLevel = (float)this.parameters["WhiteLevel"].GetValue();

            // render gbuffer to hdr buffer
            this.hdrExposure.BindForWriting();

            perfmon.Start("RenderGBufferCombiner");
            RenderGBufferCombiner();
            perfmon.Stop("RenderGBufferCombiner");

            this.hdrExposure.UnbindFromWriting();



            // render hdr buffer to screen

            GL.Viewport(this.ClientRectangle);
            GL.ClearColor(0.0f, 0.0f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            perfmon.Start("HDR");
            this.hdrExposure.Render();
            perfmon.Stop("HDR");


            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            perfmon.Start("RefreshText");
            if (textManager.NeedsRefresh)
            {
                textManager.Refresh();
            }
            perfmon.Stop("RefreshText");

            perfmon.Start("RenderText");
            textManager.Render(overlayProjection, overlayModelview);
            perfmon.Stop("RenderText");
            GL.Enable(EnableCap.DepthTest);

            //GL.Flush();

            SwapBuffers();

            this.frameCounter.Frame();

        }

        private void RenderLighting(Vector3 sunVector)
        {
            this.terrainLighting.Render(sunVector, this.terrainGlobal.HeightTexture, this.terrainGlobal.MinHeight, this.terrainGlobal.MaxHeight);
        }

        private void RenderCloudDepth(Vector3 sunVector)
        {
            this.cloudDepthRenderer.Render(this.cloudTexture, sunVector, this.cloudScale);
        }

        private void RenderSky(Vector3 eyePos, Vector3 sunVector, float groundLevel)
        {
            this.skyRenderer.Render(
                this.skyCubeTexture,
                eyePos,
                sunVector,
                groundLevel,
                (float)this.parameters["raleighBrightness"].GetValue(),
                (float)this.parameters["mieBrightness"].GetValue(),
                (float)this.parameters["scatterAbsorb"].GetValue(),
                new Vector3(
                    (float)this.parameters["Kr_r"].GetValue(),
                    (float)this.parameters["Kr_g"].GetValue(),
                    (float)this.parameters["Kr_b"].GetValue()
                ),
                new Vector3(
                        (float)this.parameters["Sun_r"].GetValue(),
                        (float)this.parameters["Sun_g"].GetValue(),
                        (float)this.parameters["Sun_b"].GetValue()
                    )
                );
        }

        private void RenderSkyRayDirections()
        {
            this.skyRayDirectionRenderer.Render(this.terrainProjection, this.terrainModelview, this.eyePos);
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
