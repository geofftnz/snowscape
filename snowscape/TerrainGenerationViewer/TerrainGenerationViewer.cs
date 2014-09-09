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
using Snowscape.TerrainRenderer.Mesh;
using Loaders = Snowscape.TerrainRenderer.Loaders;
using OpenTKExtensions.Framework;


namespace Snowscape.TerrainGenerationViewer
{
    public class TerrainGenerationViewer : GameWindow
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        const int TileWidth = 1024;
        const int TileHeight = 1024;

        const int SkyRes = 512;
        const int DetailRes = 1024;
        const int TerrainParticleRes = 512;

        private GameComponentCollection components = new GameComponentCollection();
        public GameComponentCollection Components
        {
            get { return this.components; }
        }

        #region Components

        private TerrainLightingGenerator terrainLighting;
        
        #endregion






        public ITerrainGen Terrain { get; set; }
        private int TerrainGenPass = 1;

        private Matrix4 overlayProjection = Matrix4.Identity;
        private Matrix4 overlayModelview = Matrix4.Identity;

        private Matrix4 terrainProjection = Matrix4.Identity;
        private Matrix4 terrainModelview = Matrix4.Identity;

        private Lighting.LightingCombiner lightingStep = new Lighting.LightingCombiner();

        private IPatchCache patchCache = new PatchCache();

        private TerrainTile terrainTile;
        private TerrainGlobal terrainGlobal;
        private ITileRenderer tileRenderer;
        private ITileRenderer tileRendererRaycast;
        private ITileRenderer tileRendererPatchDetail;
        private ITileRenderer tileRendererPatchNormal;
        private ITileRenderer tileRendererPatchLow;
        private ITileRenderer tileRendererLOD;
        private ITileRenderer tileRendererQuadtree;
        private Atmosphere.RayDirectionRenderer skyRayDirectionRenderer = new Atmosphere.RayDirectionRenderer();
        private Lighting.HeightmapNormalGenerator tileNormalGenerator = new Lighting.HeightmapNormalGenerator();
        private Lighting.IndirectIlluminationGenerator indirectIlluminationGenerator = new Lighting.IndirectIlluminationGenerator();

        private HDR.HDRExposureMapper hdrExposure = new HDR.HDRExposureMapper();

        private Loaders.TerrainGlobalLoader terrainGlobalLoader = new Loaders.TerrainGlobalLoader();
        private Loaders.TerrainTileLoader terrainTileLoader = new Loaders.TerrainTileLoader();
        private Loaders.TerrainTileParamLoader terrainTileParamLoader = new Loaders.TerrainTileParamLoader();

        private GBufferSimpleStep terrainSnowGlobalLoader;
        private GBufferSimpleStep terrainSnowTileLoader;
        private GBufferSimpleStep terrainSnowTileParamLoader;


        //private Texture skyTexture;
        private Atmosphere.SkyScatteringCubeRenderer skyRenderer = new Atmosphere.SkyScatteringCubeRenderer(SkyRes);

        private Texture skyCubeTexture;

        private Texture terrainDetailTexture;

        private Vector3 sunDirection = Vector3.Normalize(new Vector3(0.8f, 0.15f, 0.6f));
        private Vector3 prevSunDirection = Vector3.Zero;

        private ParameterCollection parameters = new ParameterCollection();

        private Vector3 eyePos;

        private ICamera camera;


        uint updateCounter = 0;
        private Font font = new Font();
        private TextManager textManager = new TextManager("DefaultText", null);

        private FrameCounter2 frameCounter = new FrameCounter2();
        private TextBlock frameCounterText = new TextBlock("fps", "", new Vector3(0.01f, 0.05f, 0.0f), 0.0003f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));


        private bool pauseUpdate = false;

        private uint updateGPUIterations = 0;
        private uint updateThreadIterations = 0;
        private uint prevThreadIterations;
        private double updateThreadUpdateTime = 0.0;
        private long waterIterations = 0;
        private int textureUpdateCount = 0;
        private uint currentParamsVersion = 0;
        private uint prevParamsVersion = 0;

        private PerfMonitor perfmon = new PerfMonitor();
        private FrameTracker frameTracker = new FrameTracker();
        private OpenTKExtensions.UI.FrameTimeGraphRenderer frameTrackerRenderer = new OpenTKExtensions.UI.FrameTimeGraphRenderer(FrameTracker.BUFLEN, FrameTracker.MAXSTEPS);

        private string terrainPath = @"../../../../terrains/";


        private bool reloadShaders = false;

        public class CloseEventArgs : EventArgs { }
        public delegate void CloseEventHandler(object source, CloseEventArgs e);
        public event CloseEventHandler OnClose;

        public TerrainGenerationViewer()
            : base(800, 600, new GraphicsMode(new ColorFormat(8, 8, 8, 8), 24, 8), "Snowscape", GameWindowFlags.Default, DisplayDevice.Default, 3, 1, GraphicsContextFlags.Default)
        {
            this.VSync = VSyncMode.Off;

            // set default shader loader
            ShaderProgram.DefaultLoader = new OpenTKExtensions.Loaders.FileSystemLoader(@"../../../Resources/Shaders");


            //this.Terrain = new TerrainGen(TileWidth, TileHeight);
            //this.Terrain = new GPUWaterErosion(TileWidth, TileHeight);
            this.Terrain = new GPUParticleErosion(TileWidth, TileHeight, TerrainParticleRes, TerrainParticleRes);


            this.terrainTile = new TerrainTile(TileWidth, TileHeight);
            this.terrainGlobal = new TerrainGlobal(TileWidth, TileHeight);
            this.tileRenderer = new GenerationVisMeshRenderer(TileWidth, TileHeight);
            this.tileRendererRaycast = new GenerationVisRaycastRenderer();
            this.tileRendererPatchDetail = new GenerationVisPatchDetailRenderer(TileWidth, TileHeight, patchCache);
            this.tileRendererPatchNormal = new GenerationVisPatchRenderer(TileWidth, TileHeight, patchCache);
            this.tileRendererPatchLow = new GenerationVisPatchLowRenderer(TileWidth, TileHeight, patchCache);
            this.tileRendererLOD = new CompositeLODRenderer(this.tileRendererRaycast, this.tileRenderer, this.tileRendererPatchDetail);
            //this.tileRendererQuadtree = new QuadtreeLODRenderer(this.tileRendererRaycast, this.tileRendererPatchLow, (IPatchRenderer)this.tileRendererPatchLow, (IPatchRenderer)this.tileRendererPatchDetail);
            this.tileRendererQuadtree = new QuadtreeLODRenderer(this.tileRendererPatchLow, this.tileRendererPatchLow, (IPatchRenderer)this.tileRendererPatchNormal, (IPatchRenderer)this.tileRendererPatchDetail);



            this.terrainLighting = new TerrainLightingGenerator(TileWidth, TileHeight);
            this.Components.Add(this.terrainLighting);



            this.camera = new WalkCamera(this.Keyboard, this.Mouse);

            this.UpdateFrame += new EventHandler<FrameEventArgs>(TerrainGenerationViewer_UpdateFrame);
            this.RenderFrame += new EventHandler<FrameEventArgs>(TerrainGenerationViewer_RenderFrame);
            this.Load += new EventHandler<EventArgs>(TerrainGenerationViewer_Load);
            this.Unload += new EventHandler<EventArgs>(TerrainGenerationViewer_Unload);
            this.Resize += new EventHandler<EventArgs>(TerrainGenerationViewer_Resize);
            this.Closed += new EventHandler<EventArgs>(TerrainGenerationViewer_Closed);
            this.Closing += new EventHandler<System.ComponentModel.CancelEventArgs>(TerrainGenerationViewer_Closing);

            this.Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(Keyboard_KeyDown);

            parameters.Add(new Parameter<bool>("glFinish", false, false, true, v => true, v => false));

            parameters.Add(new Parameter<float>("exposure", -1.0f, -100.0f, -0.0005f, v => v * 1.05f, v => v * 0.95f));
            parameters.Add(new Parameter<float>("WhiteLevel", 10.0f, 0.05f, 100.0f, v => v += 0.05f, v => v -= 0.05f));
            parameters.Add(new Parameter<float>("BlackLevel", 0.0f, 0.0f, 100.0f, v => v += 0.01f, v => v -= 0.01f));



            parameters.Add(new Parameter<float>("sunElevation", 0.2f, -1.0f, 1.0f, v => v + 0.005f, v => v - 0.005f, ParameterImpact.PreCalcLighting));
            parameters.Add(new Parameter<float>("sunAzimuth", 0.2f, 0.0f, 1.0f, v => v + 0.01f, v => v - 0.01f, ParameterImpact.PreCalcLighting));

            //vec3(0.100,0.598,0.662) * 1.4
            //0.18867780436772762, 0.4978442963618773, 0.6616065586417131
            parameters.Add(new Parameter<float>("Kr_r", 0.1287f, 0.0f, 1.0f, v => v + 0.002f, v => v - 0.002f, ParameterImpact.PreCalcLighting));
            parameters.Add(new Parameter<float>("Kr_g", 0.1898f, 0.0f, 1.0f, v => v + 0.002f, v => v - 0.002f, ParameterImpact.PreCalcLighting));
            parameters.Add(new Parameter<float>("Kr_b", 0.7216f, 0.0f, 1.0f, v => v + 0.002f, v => v - 0.002f, ParameterImpact.PreCalcLighting));  // 0.6616
            parameters.Add(new Parameter<float>("Sun_r", 5.0f, 0.0f, 16.0f, v => v + 0.02f, v => v - 0.02f, ParameterImpact.PreCalcLighting));
            parameters.Add(new Parameter<float>("Sun_g", 4.4f, 0.0f, 16.0f, v => v + 0.02f, v => v - 0.02f, ParameterImpact.PreCalcLighting));
            parameters.Add(new Parameter<float>("Sun_b", 4.0f, 0.0f, 16.0f, v => v + 0.02f, v => v - 0.02f, ParameterImpact.PreCalcLighting));  // 0.6616

            parameters.Add(new Parameter<float>("scatterAbsorb", 0.3833f, 0.0001f, 4.0f, v => v * 1.02f, v => v * 0.98f));  // 0.028  0.1

            parameters.Add(new Parameter<float>("mieBrightness", 0.005f, 0.0001f, 40.0f, v => v * 1.02f, v => v * 0.98f, ParameterImpact.PreCalcLighting));
            parameters.Add(new Parameter<float>("miePhase", 0.99f, 0.0f, 1.0f, v => v + 0.001f, v => v - 0.001f));
            parameters.Add(new Parameter<float>("raleighBrightness", 0.03f, 0.0001f, 40.0f, v => v * 1.02f, v => v * 0.98f, ParameterImpact.PreCalcLighting));
            parameters.Add(new Parameter<float>("skylightBrightness", 3.8f, 0.0001f, 40.0f, v => v * 1.02f, v => v * 0.98f));
            parameters.Add(new Parameter<float>("AOInfluenceHeight", 5.0f, 0.5f, 2000.0f, v => v + 0.5f, v => v - 0.5f));

            parameters.Add(new Parameter<float>("sampleDistanceFactor", 0.0003f, 0.0000001f, 1.0f, v => v * 1.05f, v => v * 0.95f));

            parameters.Add(new Parameter<float>("groundLevel", 0.985f, 0.5f, 0.99999f, v => v + 0.0001f, v => v - 0.0001f, ParameterImpact.PreCalcLighting)); // 0.995 0.98

            parameters.Add(new Parameter<float>("AmbientBias", 0.80f, 0.0f, 10.0f, v => v + 0.002f, v => v - 0.002f)); // 0.995 0.98
            parameters.Add(new Parameter<float>("IndirectBias", 0.05f, 0.0f, 10.0f, v => v + 0.005f, v => v - 0.005f)); // 0.995 0.98

            //parameters.Add(new Parameter<float>("cloudLevel", 250.0f, -1000.0f, 1000.0f, v => v + 1f, v => v - 1f));
            //parameters.Add(new Parameter<float>("cloudThickness", 50.0f, 10.0f, 2000.0f, v => v + 5f, v => v - 5f));

            parameters.Add(new Parameter<float>("NearScatterDistance", 1200.0f, 10.0f, 20000.0f, v => v + 10f, v => v - 10f));
            parameters.Add(new Parameter<float>("NearMieBrightness", 10.0f, 0.0f, 20.0f, v => v + 0.1f, v => v - 0.1f));

            parameters.Add(new Parameter<float>("ScatteringInitialStepSize", 0.001f, 0.0001f, 10.0f, v => v + 0.0001f, v => v - 0.0001f));
            parameters.Add(new Parameter<float>("ScatteringStepGrowthFactor", 1.1f, 1.0f, 2.0f, v => v + 0.001f, v => v - 0.001f));

            parameters.Add(new Parameter<float>("SnowSlopeDepthAdjust", 0.1f, 0.0f, 100.0f, v => v * 1.02f, v => v * 0.98f));

            parameters.Add(new Parameter<float>("DetailHeightScale", 0.1f, 0.0f, 10.0f, v => v + 0.01f, v => v - 0.01f));


            parameters.Add(new Parameter<float>("NormalBlendNearDistance", 100.0f, 0.0f, 2000.0f, v => v * 1.02f, v => v * 0.98f));
            parameters.Add(new Parameter<float>("NormalBlendFarDistance", 500.0f, 0.0f, 2000.0f, v => v * 1.02f, v => v * 0.98f));
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
            if (e.Key == Key.T)
            {
                this.perfmon.ResetAll();
            }
            if (e.Key == Key.P)
            {
                this.SwitchPass();
            }

            if (e.Key == Key.Up)
            {
                this.parameters.CurrentIndex -= (this.Keyboard[Key.ShiftLeft] || this.Keyboard[Key.ShiftRight]) ? 10 : 1;
            }
            if (e.Key == Key.Down)
            {
                this.parameters.CurrentIndex += (this.Keyboard[Key.ShiftLeft] || this.Keyboard[Key.ShiftRight]) ? 10 : 1;
            }

            if (e.Key == Key.Left)
            {
                this.parameters.Current.Decrease();

                if (this.parameters.Current.Impacts(ParameterImpact.PreCalcLighting))
                {
                    currentParamsVersion++;
                }
            }
            if (e.Key == Key.Right)
            {
                this.parameters.Current.Increase();
                if (this.parameters.Current.Impacts(ParameterImpact.PreCalcLighting))
                {
                    currentParamsVersion++;
                }
            }
            if (e.Key == Key.PageUp)
            {
                for (int i = 0; i < 10; i++)
                {
                    this.parameters.Current.Increase();
                }
                if (this.parameters.Current.Impacts(ParameterImpact.PreCalcLighting))
                {
                    currentParamsVersion++;
                }
            }
            if (e.Key == Key.PageDown)
            {
                for (int i = 0; i < 10; i++)
                {
                    this.parameters.Current.Decrease();
                }
                if (this.parameters.Current.Impacts(ParameterImpact.PreCalcLighting))
                {
                    currentParamsVersion++;
                }
            }




            if (e.Key == Key.Space)
            {
                if (pauseUpdate) // currently paused
                {
                    this.pauseUpdate = false;
                }
                else // currently running
                {
                    this.pauseUpdate = true;
                }
            }

            if (e.Key == Key.L)
            {
                reloadShaders = true;
            }
        }

        private void SwitchPass()
        {
            switch (this.TerrainGenPass)
            {
                case 1: // currently on pass 0, start on pass 1
                    {
                        this.Terrain.Save(GetTerrainFileName(0, 1));
                        GPUSnowTransport newgen = new GPUSnowTransport(this.Terrain.Width, this.Terrain.Height, TerrainParticleRes, TerrainParticleRes);
                        newgen.Init();
                        newgen.InitFromPass1(this.Terrain);
                        this.Terrain.Unload();
                        this.Terrain = newgen;
                        this.TerrainGenPass = 2;

                        // todo: this needs to be moved out of there into a generic on-init-generation-step proc
                        foreach (var p in this.Terrain.GetParameters())
                        {
                            this.parameters.Add(p);
                        }
                    }
                    break;

                case 2:
                    {
                        this.Terrain.Save(GetTerrainFileName(0, 2));
                        //GPUWaterErosion newgen = new GPUWaterErosion(this.Terrain.Width, this.Terrain.Height);
                        var newgen = new GPUParticleErosion(TileWidth, TileHeight, TerrainParticleRes, TerrainParticleRes);
                        newgen.Init();
                        newgen.Load(GetTerrainFileName(0, 1));

                        this.Terrain.Unload();
                        this.Terrain = newgen;
                        this.TerrainGenPass = 1;


                    }
                    break;
                default: break;
            }


        }



        void TerrainGenerationViewer_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            log.Trace("Saving terrain into slot 0...");
            this.Terrain.Save(this.GetTerrainFileName(0, this.TerrainGenPass));
        }

        void TerrainGenerationViewer_Closed(object sender, EventArgs e)
        {
            OnClose(this, new CloseEventArgs());
        }


        void TerrainGenerationViewer_Load(object sender, EventArgs e)
        {

            // create VBOs/Shaders etc

            this.terrainTile.Load();
            this.terrainGlobal.Init();
            this.tileRenderer.Load();
            this.tileRendererRaycast.Load();
            this.tileRendererPatchDetail.Load();
            this.tileRendererPatchNormal.Load();
            this.tileRendererPatchLow.Load();
            this.tileRendererLOD.Load();
            this.tileRendererQuadtree.Load();

            //this.terrainLighting.Init(this.terrainGlobal.ShadeTexture);
            this.terrainLighting.OutputTexture = this.terrainGlobal.ShadeTexture;



            this.tileNormalGenerator.Init(this.terrainTile.NormalTexture);
            this.indirectIlluminationGenerator.Init(this.terrainGlobal.IndirectIlluminationTexture);

            this.terrainGlobalLoader.Init(this.terrainGlobal.HeightTexture);
            this.terrainTileLoader.Init(this.terrainTile.HeightTexture);
            this.terrainTileParamLoader.Init(this.terrainTile.ParamTexture);

            terrainSnowGlobalLoader = new GBufferSimpleStep("snow-global-loader", @"TileLoader.glsl|SnowGlobal", "terraintex", "out_Height", this.terrainGlobal.HeightTexture);
            terrainSnowTileLoader = new GBufferSimpleStep("snow-tile-loader", @"TileLoader.glsl|SnowTile", "terraintex", "out_Height", this.terrainTile.HeightTexture);
            terrainSnowTileParamLoader = new GBufferSimpleStep("snow-param-loader", @"TileLoader.glsl|SnowParam", "terraintex", "out_Param", this.terrainTile.ParamTexture);


            //this.gbuffer.SetSlot(0, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // pos
            //this.gbuffer.SetSlot(1, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // param
            //this.gbuffer.Init(this.ClientRectangle.Width, this.ClientRectangle.Height);

            //var program = new ShaderProgram("combiner");

            //program.Init(
            //    @"GBufferVisCombine.vert",
            //    @"GBufferVisCombine.frag",
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


            this.terrainDetailTexture = new Texture(DetailRes, DetailRes, TextureTarget.Texture2D, PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float);
            this.terrainDetailTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat));
            this.terrainDetailTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));
            this.terrainDetailTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
            this.terrainDetailTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear));

            TextureSynth ts = new TextureSynth(DetailRes, DetailRes);

            this.terrainDetailTexture.Upload(ts.ApplyWrapNoise(6, 8.0f, 1.0f, h => Math.Abs(h), h => h).Normalise().GetData());
            /*
            this.terrainDetailTexture.Upload(
                ts.ForEach((x, y, h) =>
                    {
                        return (float)(((x>>4) & 0x01) ^ ((y>>4) & 0x01));
                    })
                    .Normalise()
                    .GetData());*/

            this.terrainDetailTexture.Bind();
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            (this.tileRendererPatchDetail as GenerationVisPatchDetailRenderer).Maybe(r => { r.DetailTexture = terrainDetailTexture; });
            (this.tileRendererPatchNormal as GenerationVisPatchRenderer).Maybe(r => { r.DetailTexture = terrainDetailTexture; });

            /*
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
            */
            // GL state
            GL.Enable(EnableCap.DepthTest);

            // setup font
            font.Init(Resources.FontConsolas, Resources.FontConsolasMeta);
            textManager.Font = font;

            SetProjection();


            // setup terrain
            this.Terrain.Init();

            try
            {
                this.Terrain.Load(this.GetTerrainFileName(0));
            }
            catch (FileNotFoundException)
            {
                this.Terrain.ResetTerrain();
            }

            foreach (var p in this.Terrain.GetParameters())
            {
                this.parameters.Add(p);
            }


            // load components
            this.Components.Load();
            

            this.frameCounter.Start();

            this.CalculateSunDirection();

            this.frameTrackerRenderer.Init();
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
            pos.Y = this.Terrain.GetHeightAt(pos.X, pos.Z);
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
            this.Terrain.ResetTerrain();
        }


        private void RenderGBufferCombiner()
        {
            Matrix4 mv = this.terrainModelview;
            //mv.M41 = 0f;
            //mv.M42 = 0f;
            //mv.M43 = 0f;
            //mv.M14 = 0f;
            //mv.M24 = 0f;
            //mv.M34 = 0f;

            Texture miscTexture = this.terrainGlobal.ShadeTexture;
            Texture miscTexture2 = this.terrainGlobal.ShadeTexture;
            if (this.Terrain is GPUParticleErosion)
            {
                miscTexture = ((GPUParticleErosion)this.Terrain).ErosionTex;
                miscTexture2 = ((GPUParticleErosion)this.Terrain).CurrentParticleTexture;
            };
            if (this.Terrain is GPUSnowTransport)
            {
                miscTexture = ((GPUSnowTransport)this.Terrain).ErosionTex;
                //miscTexture2 = ((GPUSnowTransport)this.Terrain).CurrentDensityTexture;
            };
            frameTracker.Step("scattering-texsetup", new Vector4(0.0f, 0.5f, 1.0f, 1.0f));


            var rp = new Lighting.LightingCombiner.RenderParams()
            {
                //GBufferProjectionMatrix = Matrix4.Mult(mv, this.terrainProjection),
                GBufferProjectionMatrix = Matrix4.Invert(Matrix4.Mult(this.terrainModelview, this.terrainProjection)),
                DepthTexture = this.lightingStep.DepthTexture,
                HeightTexture = this.terrainGlobal.HeightTexture,
                ShadeTexture = this.terrainGlobal.ShadeTexture,
                SkyCubeTexture = this.skyCubeTexture,
                IndirectIlluminationTexture = this.terrainGlobal.IndirectIlluminationTexture,
                EyePos = this.eyePos,
                SunDirection = this.sunDirection,
                MinHeight = this.terrainGlobal.MinHeight,
                MaxHeight = this.terrainGlobal.MaxHeight,
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
                MiePhase = (float)this.parameters["miePhase"].GetValue(),
                RaleighBrightness = (float)this.parameters["raleighBrightness"].GetValue(),
                SkylightBrightness = (float)this.parameters["skylightBrightness"].GetValue(),
                GroundLevel = (float)this.parameters["groundLevel"].GetValue(),
                TileWidth = this.terrainTile.Width,
                TileHeight = this.terrainTile.Height,
                SampleDistanceFactor = (float)this.parameters["sampleDistanceFactor"].GetValue(),
                NearScatterDistance = (float)this.parameters["NearScatterDistance"].GetValue(),
                NearMieBrightness = (float)this.parameters["NearMieBrightness"].GetValue(),
                AOInfluenceHeight = (float)this.parameters["AOInfluenceHeight"].GetValue(),
                AmbientBias = (float)this.parameters["AmbientBias"].GetValue(),
                IndirectBias = (float)this.parameters["IndirectBias"].GetValue(),

                SnowSlopeDepthAdjust = (float)this.parameters["SnowSlopeDepthAdjust"].GetValue(),

                ScatteringInitialStepSize = (float)this.parameters["ScatteringInitialStepSize"].GetValue(),
                ScatteringStepGrowthFactor = (float)this.parameters["ScatteringStepGrowthFactor"].GetValue(),
                Time = (float)(this.frameCounter.Frames % 65536),

                //MiscTexture = (this.Terrain is GPUWaterErosion) ? ((GPUWaterErosion)this.Terrain).VelocityTex : this.terrainGlobal.ShadeTexture,
                //MiscTexture2 = (this.Terrain is GPUWaterErosion) ? ((GPUWaterErosion)this.Terrain).VisTex : this.terrainGlobal.ShadeTexture,
                MiscTexture = miscTexture,
                MiscTexture2 = miscTexture2,

                RenderMode = (float)this.TerrainGenPass,

                NormalBlendNearDistance = (float)this.parameters["NormalBlendNearDistance"].GetValue(),
                NormalBlendFarDistance = (float)this.parameters["NormalBlendFarDistance"].GetValue()

            };
            frameTracker.Step("scattering-params", new Vector4(0.5f, 0.0f, 1.0f, 1.0f));

            this.lightingStep.Render(rp);

        }


        void TerrainGenerationViewer_RenderFrame(object sender, FrameEventArgs e)
        {
            bool needToRenderLighting = false;
            bool stepFence = (bool)this.parameters["glFinish"].GetValue();

            if (this.reloadShaders)
            {
                this.lightingStep.ReloadShader();
                this.reloadShaders = false;
            }

            frameTracker.Step("interframe", new Vector4(0.8f, 0.8f, 0.8f, 1.0f));
            frameTracker.StartFrame();


            //if (this.frameCounter.Frames % 32 == 0)
            //{
            frameCounterText.Text = string.Format("FPS: {0:0} Upd:{1:###0} {2:0.0}ms Water:{3:#,###,###,##0}", frameCounter.FPS, this.updateThreadIterations, this.updateThreadUpdateTime, this.waterIterations);
            textManager.AddOrUpdate(frameCounterText);
            frameTracker.Step("text-fps", new Vector4(0.8f, 0.0f, 0.0f, 1.0f));

            float y = 0.1f;

            textManager.RemoveAllByPrefix("perf_");
            foreach (var timer in this.frameTracker.GetStepStats())
            {
                textManager.AddOrUpdate(new TextBlock("perf_" + timer.Name, string.Format("{0}: {1:0.000} ms", timer.Name, timer.AverageTime * 1000.0), new Vector3(0.01f, y, 0.0f), 0.00025f, timer.Colour));
                y += 0.0125f;
            }
            frameTracker.Step("text-timers", new Vector4(0.8f, 0.0f, 0.0f, 1.0f));

            //foreach (var timer in this.perfmon.AllAverageTimes())
            //{
            //    textManager.AddOrUpdate(new TextBlock("perf" + timer.Item1, string.Format("{0}: {1:0.000} ms", timer.Item1, timer.Item2), new Vector3(0.01f, y, 0.0f), 0.00025f, new Vector4(1.0f, 1.0f, 1.0f, 0.5f)));
            //    y += 0.0125f;
            //}

            y += 0.02f;
            for (int i = 0; i < this.parameters.DisplayLength; i++)  //this.parameters.Count
            {
                int paramindex = i + this.parameters.DisplayOffset;

                if (paramindex >= 0 && paramindex < this.parameters.Count)
                {
                    textManager.AddOrUpdate(
                        new TextBlock(
                            "p_" + this.parameters[i].Name,
                            this.parameters[paramindex].ToString(),
                            new Vector3(0.01f, y, 0.0f),
                            0.0004f,
                            new Vector4(1.0f, 0.7f, 0.0f, (paramindex == this.parameters.CurrentIndex) ? 1.0f : 0.7f)
                            )
                        );
                    y += 0.025f;
                }
            }

            frameTracker.Step("text-params", new Vector4(0.8f, 0.0f, 0.0f, 1.0f));


            if (!this.pauseUpdate)
            {
                // do GPU-based terrain generation
                for (int i = 0; i < 1; i++)
                {
                    this.Terrain.ModifyTerrain();
                    this.updateGPUIterations++;
                }
                this.updateThreadIterations = this.updateGPUIterations;
                if (stepFence) { GL.Finish(); }
                frameTracker.Step("terrain modify", new Vector4(0.8f, 0.6f, 0.0f, 1.0f));
            }

            uint currentThreadIterations = updateThreadIterations;
            if (prevThreadIterations != currentThreadIterations)
            {

                // TODO: fix
                if (this.Terrain is GPUWaterErosion)
                {
                    var terr = this.Terrain as GPUWaterErosion;
                    this.terrainGlobalLoader.Render(terr.CurrentTerrainTexture);
                    this.terrainTileLoader.Render(terr.CurrentTerrainTexture);
                    this.terrainTileParamLoader.Render(terr.CurrentTerrainTexture);
                }
                if (this.Terrain is GPUParticleErosion)
                {
                    var terr = this.Terrain as GPUParticleErosion;
                    this.terrainGlobalLoader.Render(terr.CurrentTerrainTexture, terr.WaterHeightFactor);
                    this.terrainTileLoader.Render(terr.CurrentTerrainTexture, terr.WaterHeightFactor);
                    this.terrainTileParamLoader.Render(terr.CurrentTerrainTexture);
                }

                if (this.Terrain is GPUSnowTransport)
                {
                    var terr = this.Terrain as GPUSnowTransport;
                    this.terrainSnowGlobalLoader.Render(terr.CurrentTerrainTexture);
                    this.terrainSnowTileLoader.Render(terr.CurrentTerrainTexture);
                    this.terrainSnowTileParamLoader.Render(terr.CurrentTerrainTexture);
                }
                frameTracker.Step("GPU data copy", new Vector4(1.0f, 1.0f, 0.0f, 1.0f));

                this.tileNormalGenerator.Render(this.terrainGlobal.HeightTexture);
                if (stepFence) { GL.Finish(); }
                frameTracker.Step("GPU normals", new Vector4(0.7f, 1.0f, 0.0f, 1.0f));

                textureUpdateCount++;
                prevThreadIterations = currentThreadIterations;

                if (textureUpdateCount % 256 == 0)
                {
                    needToRenderLighting = true;
                }
            }

            this.CalculateSunDirection();
            if (prevSunDirection != sunDirection || prevParamsVersion != currentParamsVersion || needToRenderLighting)
            {
                // render lighting
                this.RenderLighting(this.sunDirection);
                if (stepFence) { GL.Finish(); }
                frameTracker.Step("lighting", new Vector4(0.3f, 1.0f, 0.0f, 1.0f));

                // render indirect lighting
                this.indirectIlluminationGenerator.Render(this.terrainGlobal.HeightTexture, this.terrainGlobal.ShadeTexture, this.terrainTile.NormalTexture, this.sunDirection);
                if (stepFence) { GL.Finish(); }
                frameTracker.Step("indirect", new Vector4(0.0f, 0.9f, 0.0f, 1.0f));

                this.RenderSky(this.eyePos, this.sunDirection, (float)this.parameters["groundLevel"].GetValue());
                if (stepFence) { GL.Finish(); }
                frameTracker.Step("sky", new Vector4(0.0f, 1.0f, 0.4f, 1.0f));

                this.prevSunDirection = this.sunDirection;
                this.prevParamsVersion = this.currentParamsVersion;
            }

            SetTerrainProjection();


            this.lightingStep.BindForWriting();

            RenderTiles();
            if (stepFence) { GL.Finish(); }
            frameTracker.Step("terrain render", new Vector4(0.0f, 0.8f, 1.0f, 1.0f));

            RenderSkyRayDirections();
            if (stepFence) { GL.Finish(); }
            frameTracker.Step("skyray render", new Vector4(0.0f, 0.9f, 0.0f, 1.0f));

            this.lightingStep.UnbindFromWriting();
            frameTracker.Step("render-completion", new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
          

            //this.hdrExposure.TargetLuminance = (float)this.parameters["TargetLuminance"].GetValue();
            this.hdrExposure.WhiteLevel = (float)this.parameters["WhiteLevel"].GetValue();
            this.hdrExposure.BlackLevel = (float)this.parameters["BlackLevel"].GetValue();
            this.hdrExposure.Exposure = (float)this.parameters["exposure"].GetValue();

            // render gbuffer to hdr buffer
            this.hdrExposure.BindForWriting();

            RenderGBufferCombiner();

            this.hdrExposure.UnbindFromWriting();

            if (stepFence) { GL.Finish(); }
            frameTracker.Step("scattering", new Vector4(0.0f, 0.0f, 1.0f, 1.0f));
            
            // render hdr buffer to screen

            GL.Viewport(this.ClientRectangle);
            GL.ClearColor(0.0f, 0.0f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            frameTracker.Step("frame clear", new Vector4(0.5f, 0.0f, 1.0f, 1.0f));

            this.hdrExposure.Render();

            if (stepFence) { GL.Finish(); }
            frameTracker.Step("HDR", new Vector4(0.5f, 0.0f, 1.0f, 1.0f));

            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            if (textManager.NeedsRefresh)
            {
                textManager.Refresh();
            }

            textManager.Render(overlayProjection, overlayModelview);
            frameTracker.Step("text-render", new Vector4(1.0f, 0.0f, 0.8f, 1.0f));

            //perfmon.Start("FrameTracker");
            //this.frameTrackerRenderer.SetData(this.frameTracker.GetBarData(), this.frameTracker.GetBarColour());
            //perfmon.Stop("FrameTracker");

            this.frameTrackerRenderer.Clear();
            foreach (var quad in this.frameTracker.GetQuads())
            {
                this.frameTrackerRenderer.AddQuad(
                    new Vector3(quad.X, 1.0f - quad.Y * 4.0f, 0.0f),
                    new Vector3(quad.Width, 0.0f, 0.0f),
                    new Vector3(0.0f, -quad.Height * 4.0f, 0.0f),
                    quad.Colour);
            }

            this.frameTrackerRenderer.Render(overlayProjection, overlayModelview, 0.5f);
            frameTracker.Step("frametracker-render", new Vector4(1.0f, 0.0f, 0.4f, 1.0f));

            GL.Enable(EnableCap.DepthTest);

            if (stepFence) { GL.Finish(); }
            SwapBuffers();

            frameTracker.Step("swapbuffers", new Vector4(1.0f, 1.0f, 0.4f, 1.0f));

            this.frameCounter.Frame();

            //Thread.Sleep(0);
        }

        private void RenderLighting(Vector3 sunVector)
        {
            //this.terrainLighting.Render(sunVector, this.terrainGlobal.HeightTexture, this.terrainGlobal.MinHeight, this.terrainGlobal.MaxHeight);
            this.terrainLighting.SunVector = sunVector;
            this.terrainLighting.HeightTexture = this.terrainGlobal.HeightTexture;
            this.terrainLighting.MaxTerrainHeight = 1000.0f;

            this.terrainLighting.Render(new FrameRenderData()); ;
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

            //RenderTile(this.terrainTile, -1f, -1f, this.tileRendererRaycast);
            //RenderTile(this.terrainTile, -1f, 0f, this.tileRendererRaycast);
            //RenderTile(this.terrainTile, -1f, 1f, this.tileRendererRaycast);
            //RenderTile(this.terrainTile, 0f, -1f, this.tileRendererRaycast);

            //((GenerationVisPatchRenderer)tileRendererPatch).Scale = 1.0f / 16.0f;
            //((GenerationVisPatchRenderer)tileRendererPatch).Offset = (new Vector2((float)0.0, (float)0.0) / 16.0f);
            //RenderTile(this.terrainTile, 0f, 0f, tileRendererPatch);

            /*
            for (int y = 0; y < TileLodScale; y++)
            {
                for (int x = 0; x < TileLodScale; x++)
                {
                    var tileRenderer = (x == 0 && y == 0) ? this.tileRendererPatch : this.tileRendererPatchLow;


                    ((GenerationVisPatchRenderer)tileRenderer).Scale = 1.0f / (float)TileLodScale;
                    ((GenerationVisPatchRenderer)tileRenderer).Offset = (new Vector2((float)x, (float)y) / (float)TileLodScale);
                    RenderTile(this.terrainTile, 0f, 0f, tileRenderer);
                }
            }*/




            //RenderTile(this.terrainTile, 0f, 1f, this.tileRendererRaycast);
            //RenderTile(this.terrainTile, 1f, -1f, this.tileRendererRaycast);
            //RenderTile(this.terrainTile, 1f, 0f, this.tileRendererRaycast);
            //RenderTile(this.terrainTile, 1f, 1f, this.tileRendererRaycast);

            //RenderTile(this.terrainTile, 0f, 0f, tileRendererQuadtree);

            (this.tileRendererPatchNormal as GenerationVisPatchRenderer).DetailTexScale = (float)this.parameters["DetailHeightScale"].GetValue();
            (this.tileRendererPatchDetail as GenerationVisPatchDetailRenderer).DetailTexScale = (float)this.parameters["DetailHeightScale"].GetValue();

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    RenderTile(this.terrainTile, (float)x, (float)y, tileRendererQuadtree);
                }
            }

        }

        private void RenderTile(TerrainTile tile, float TileXOffset, float TileZOffset, ITileRenderer renderer)
        {
            tile.ModelMatrix = Matrix4.CreateTranslation(TileXOffset * (float)tile.Width, 0f, TileZOffset * (float)tile.Height);

            //this.tileRendererLOD.Render(tile, this.terrainProjection, this.terrainModelview, this.eyePos);
            renderer.Render(tile, this.terrainProjection, this.terrainModelview, this.eyePos);
        }

        protected string GetTerrainFileName(int index)
        {
            return GetTerrainFileName(index, 1);
        }

        protected string GetTerrainFileName(int index, int pass)
        {
            return string.Format("{0}Terrain{1}.1024.pass{2}.ter", this.terrainPath, index, pass);
        }
    }
}
