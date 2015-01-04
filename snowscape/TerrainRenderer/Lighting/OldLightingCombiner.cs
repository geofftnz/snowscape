using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utils;
using NLog;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainRenderer.Lighting
{
    /// <summary>
    /// OldLightingCombiner
    /// 
    /// Takes the output of the rasterising/raycasting step (GBuffer of position/composition etc) and
    /// performs the colour generation and lighting. This is output to a FP16 colour buffer, ready for 
    /// exposure and gamma correction. Output texture will have mipmaps generated on it so that it can 
    /// be read back for exposure calculation.
    /// 
    /// Input:
    /// slot 0: Position texture
    /// slot 1: Param texture
    /// 
    /// Output:
    /// colour
    /// 
    /// Knows about:
    /// 
    /// Knows how to:
    /// - render the gbuffer from the previous step into a new colour buffer
    /// 
    /// </summary>
    public class OldLightingCombiner : GameComponentBase
    {
        private GBuffer gbuffer = new GBuffer("lighting", true);
        private ShaderProgram program = new ShaderProgram("combiner");
        private GBufferCombiner gbufferCombiner;
        private Matrix4 projection = Matrix4.Identity;
        private Matrix4 modelview = Matrix4.Identity;

        private static Logger log = LogManager.GetCurrentClassLogger();

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Texture DepthTexture
        {
            get
            {
                return gbuffer.DepthTexture;
            }
        }

        /// <summary>
        /// Parameters for lighting step
        /// </summary>
        public class RenderParams
        {
            public Matrix4 GBufferProjectionMatrix { get; set; }
            public Texture DepthTexture { get; set; }
            public Texture HeightTexture { get; set; }
            public Texture ShadeTexture { get; set; }
            public Texture NoiseTexture { get; set; }
            public Texture SkyCubeTexture { get; set; }
            public Texture IndirectIlluminationTexture { get; set; }
            public Vector3 EyePos { get; set; }
            public Vector3 SunDirection { get; set; }
            public float MinHeight { get; set; }
            public float MaxHeight { get; set; }
            public float Exposure { get; set; }
            public Vector3 Kr { get; set; }
            public float ScatterAbsorb { get; set; }
            public float MieBrightness { get; set; }
            public float MiePhase { get; set; }
            public float RaleighBrightness { get; set; }
            public float SkylightBrightness { get; set; }
            public float GroundLevel { get; set; }
            public int TileWidth { get; set; }
            public int TileHeight { get; set; }
            public Vector3 SunLight { get; set; }
            public float SampleDistanceFactor { get; set; }
            public float NearScatterDistance { get; set; }
            public float NearMieBrightness { get; set; }
            public float AOInfluenceHeight { get; set; }
            public float ScatteringInitialStepSize { get; set; }
            public float ScatteringStepGrowthFactor { get; set; }
            public float Time { get; set; }
            public float AmbientBias { get; set; }
            public float IndirectBias { get; set; }

            public Texture MiscTexture { get; set; }
            public Texture MiscTexture2 { get; set; }

            public float RenderMode { get; set; }

            public float SnowSlopeDepthAdjust { get; set; }

            public float NormalBlendNearDistance { get; set; }
            public float NormalBlendFarDistance { get; set; }

            public RenderParams()
            {
            }
        }


        public OldLightingCombiner()
            : base()
        {

            
            this.Loading += LightingCombiner_Loading;
        }


        public OldLightingCombiner(int width, int height)
            : this()
        {
            this.Width = width;
            this.Height = height;
        }

        void LightingCombiner_Loading(object sender, EventArgs e)
        {
            this.gbuffer.SetSlot(0, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // rgb+shadow
            this.gbuffer.SetSlot(1, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // rough+specexp+specpwr+sparkle
            this.gbuffer.SetSlot(2, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // normal

            this.gbuffer.Init(this.Width, this.Height);

            InitShader(program);

            this.gbufferCombiner = new GBufferCombiner(this.gbuffer, this.program);

            this.projection = Matrix4.CreateOrthographicOffCenter(0.0f, 1.0f, 1.0f, 0.0f, 0.001f, 10.0f);
            this.modelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);
        }

        private void InitShader(ShaderProgram program)
        {
            program.Init(
                @"GBufferVisCombine.vert",
                @"GBufferVisCombine.frag",
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "in_texcoord0") 
                });
        }

        public void ReloadShader()
        {
            try
            {
                ShaderProgram newprogram = new ShaderProgram(this.program.Name);
                InitShader(newprogram);

                this.program.Unload();
                this.program = newprogram;
                this.gbufferCombiner.CombineProgram = this.program;

            }
            catch (Exception ex)
            {
                log.Warn("Could not replace shader program {0}: {1}", this.program.Name, ex.GetType().Name + ": " + ex.Message);
            }
        }

        public void Resize(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            this.gbuffer.Init(this.Width, this.Height);
        }


        public void BindForWriting()
        {
            this.gbuffer.BindForWriting();
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.ClearDepth(1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.ColorMask(true, true, true, true);
        }

        public void UnbindFromWriting()
        {
            this.gbuffer.UnbindFromWriting();
        }

        public void Render(RenderParams rp)
        {


            rp.HeightTexture.Bind(TextureUnit.Texture3);
            rp.ShadeTexture.Bind(TextureUnit.Texture4);
            //rp.CloudTexture.Bind(TextureUnit.Texture4);
            //rp.CloudDepthTexture.Bind(TextureUnit.Texture5);
            rp.IndirectIlluminationTexture.Bind(TextureUnit.Texture5);
            rp.SkyCubeTexture.Bind(TextureUnit.Texture6);
            rp.DepthTexture.Bind(TextureUnit.Texture7);
            rp.MiscTexture.Bind(TextureUnit.Texture8);
            rp.MiscTexture2.Bind(TextureUnit.Texture9);

            this.gbufferCombiner.Render(projection, modelview, (sp) =>
            {
                sp.SetUniform("pre_projection_matrix", rp.GBufferProjectionMatrix);
                sp.SetUniform("eyePos", rp.EyePos);
                sp.SetUniform("sunVector", rp.SunDirection);
                sp.SetUniform("paramTex", 0);
                sp.SetUniform("normalTex", 1);
                sp.SetUniform("normalLargeScaleTex", 2);
                sp.SetUniform("heightTex", 3);
                sp.SetUniform("shadeTex", 4);
                sp.SetUniform("indirectTex", 5);
                sp.SetUniform("skyCubeTex", 6);
                sp.SetUniform("depthTex", 7);
                sp.SetUniform("miscTex", 8);
                sp.SetUniform("miscTex2", 9);
                sp.SetUniform("minHeight", rp.MinHeight);
                sp.SetUniform("maxHeight", rp.MaxHeight);
                sp.SetUniform("exposure", rp.Exposure);
                sp.SetUniform("Kr", rp.Kr);
                sp.SetUniform("sunLight", rp.SunLight);
                sp.SetUniform("scatterAbsorb", rp.ScatterAbsorb);
                sp.SetUniform("mieBrightness", rp.MieBrightness);
                sp.SetUniform("miePhase", rp.MiePhase);
                sp.SetUniform("raleighBrightness", rp.RaleighBrightness);
                sp.SetUniform("skylightBrightness", rp.SkylightBrightness);
                sp.SetUniform("groundLevel", rp.GroundLevel);
                sp.SetUniform("sampleDistanceFactor", rp.SampleDistanceFactor);
                sp.SetUniform("nearScatterDistance", rp.NearScatterDistance);
                sp.SetUniform("nearMieBrightness", rp.NearMieBrightness);
                sp.SetUniform("aoInfluenceHeight", rp.AOInfluenceHeight);
                sp.SetUniform("ambientBias", rp.AmbientBias);
                sp.SetUniform("indirectBias", rp.IndirectBias);
                sp.SetUniform("renderMode", rp.RenderMode);
                sp.SetUniform("snowSlopeDepthAdjust", rp.SnowSlopeDepthAdjust);


                sp.SetUniform("normalBlendNearDistance", rp.NormalBlendNearDistance);
                sp.SetUniform("normalBlendFarDistance", rp.NormalBlendFarDistance);

                sp.SetUniform("scatteringInitialStepSize", rp.ScatteringInitialStepSize);
                sp.SetUniform("scatteringStepGrowthFactor", rp.ScatteringStepGrowthFactor);

                sp.SetUniform("time", rp.Time);

                sp.SetUniform("boxparam", new Vector4((float)rp.TileWidth, (float)rp.TileHeight, 0.0f, 1.0f));
            });
        }



    }
}
