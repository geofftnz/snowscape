using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utils;

namespace Snowscape.TerrainRenderer.Lighting
{
    /// <summary>
    /// LightingCombiner
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
    public class LightingCombiner
    {
        private GBuffer gbuffer = new GBuffer("lighting");
        private ShaderProgram program = new ShaderProgram("combiner");
        private GBufferCombiner gbufferCombiner;
        private Matrix4 projection = Matrix4.Identity;
        private Matrix4 modelview = Matrix4.Identity;

        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>
        /// Parameters for lighting step
        /// </summary>
        public class RenderParams
        {
            public Texture HeightTexture { get; set; }
            public Texture ShadeTexture { get; set; }
            public Texture CloudTexture { get; set; }
            public Texture CloudDepthTexture { get; set; }
            public Texture SkyCubeTexture { get; set; }
            public Vector3 EyePos { get; set; }
            public Vector3 SunDirection { get; set; }
            public float MinHeight { get; set; }
            public float MaxHeight { get; set; }
            public Vector3 CloudScale { get; set; }
            public float Exposure { get; set; }
            public Vector3 Kr { get; set; }
            public float ScatterAbsorb { get; set; }
            public float MieBrightness { get; set; }
            public float RaleighBrightness { get; set; }
            public float SkylightBrightness { get; set; }
            public float GroundLevel { get; set; }
            public float CloudLevel { get; set; }
            public float CloudThickness { get; set; }
            public int TileWidth { get; set; }
            public int TileHeight { get; set; }
            public Vector3 SunLight { get; set; }
            public float SampleDistanceFactor { get; set; }
            public float NearScatterDistance { get; set; }
            public float NearMieBrightness { get; set; }
            public float AOInfluenceHeight { get; set; }
            public RenderParams()
            {
            }

        }


        public LightingCombiner()
        {

        }

        public void Init(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            this.gbuffer.SetSlot(0, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // pos
            this.gbuffer.SetSlot(1, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // param
            this.gbuffer.Init(this.Width, this.Height);

            program.Init(
                @"../../../Resources/Shaders/GBufferVisCombine.vert".Load(),
                @"../../../Resources/Shaders/GBufferVisCombine.frag".Load(),
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "in_texcoord0") 
                });

            this.gbufferCombiner = new GBufferCombiner(this.gbuffer, this.program);

            this.projection = Matrix4.CreateOrthographicOffCenter(0.0f, 1.0f, 1.0f, 0.0f, 0.001f, 10.0f);
            this.modelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);


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


            rp.HeightTexture.Bind(TextureUnit.Texture2);
            rp.ShadeTexture.Bind(TextureUnit.Texture3);
            rp.CloudTexture.Bind(TextureUnit.Texture4);
            rp.CloudDepthTexture.Bind(TextureUnit.Texture5);
            rp.SkyCubeTexture.Bind(TextureUnit.Texture6);

            this.gbufferCombiner.Render(projection, modelview, (sp) =>
            {
                sp.SetUniform("eyePos", rp.EyePos);
                sp.SetUniform("sunVector", rp.SunDirection);
                sp.SetUniform("posTex", 0);
                sp.SetUniform("paramTex", 1);
                sp.SetUniform("heightTex", 2);
                sp.SetUniform("shadeTex", 3);
                sp.SetUniform("noiseTex", 4);
                sp.SetUniform("cloudDepthTex", 5);
                sp.SetUniform("skyCubeTex", 6);
                sp.SetUniform("minHeight", rp.MinHeight);
                sp.SetUniform("maxHeight", rp.MaxHeight);
                sp.SetUniform("cloudScale", rp.CloudScale);
                sp.SetUniform("exposure", rp.Exposure);
                sp.SetUniform("Kr",rp.Kr);
                sp.SetUniform("sunLight", rp.SunLight);
                sp.SetUniform("scatterAbsorb", rp.ScatterAbsorb);
                sp.SetUniform("mieBrightness", rp.MieBrightness);
                sp.SetUniform("raleighBrightness", rp.RaleighBrightness);
                sp.SetUniform("skylightBrightness", rp.SkylightBrightness);
                sp.SetUniform("groundLevel", rp.GroundLevel);
                sp.SetUniform("cloudLevel", rp.CloudLevel);
                sp.SetUniform("cloudThickness", rp.CloudThickness);
                sp.SetUniform("sampleDistanceFactor", rp.SampleDistanceFactor);
                sp.SetUniform("nearScatterDistance", rp.NearScatterDistance);
                sp.SetUniform("nearMieBrightness", rp.NearMieBrightness);
                sp.SetUniform("aoInfluenceHeight", rp.AOInfluenceHeight);
                sp.SetUniform("boxparam", new Vector4((float)rp.TileWidth, (float)rp.TileHeight, 0.0f, 1.0f));
            });
        }



    }
}
