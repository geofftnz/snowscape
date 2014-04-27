using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utils;

namespace Snowscape.TerrainRenderer.HDR
{
    public class HDRExposureMapper
    {
        private const int HISTOGRAMWIDTH = 256;

        private GBuffer gbuffer = new GBuffer("exposure");
        private ShaderProgram program = new ShaderProgram("exposure");
        private GBufferCombiner gbufferCombiner;
        private Matrix4 projection = Matrix4.Identity;
        private Matrix4 modelview = Matrix4.Identity;
        private Texture histogramTexture = new Texture("histogram", HISTOGRAMWIDTH, 1, TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);

        public IToneMapper ToneMapper { get; set; }


        public Vector4 debugCol = Vector4.Zero;

        public int Width { get; private set; }
        public int Height { get; private set; }

        //public float FastExposure { get; set; }
        //public float SlowExposure { get; set; }
        //public float Exposure
        //{
        //    get
        //    {
        //        return 0.8f.Lerp(this.SlowExposure, this.FastExposure);
        //    }
        //}

        //public float TargetLuminance { get; set; }

        public float WhiteLevel { get; set; }
        public float BlackLevel { get; set; }
        public float Exposure { get; set; }



        public HDRExposureMapper()
        {
            this.WhiteLevel = 1.0f;
            //this.FastExposure = -1.0f;
            //this.SlowExposure = -1.0f;
            //this.TargetLuminance = 0.4f;
            this.Exposure = -1.0f;
            this.BlackLevel = 0.0f;

            this.ToneMapper = new ReinhardToneMap() { WhiteLevel = 2.0f };
            //this.ToneMapper = new Uncharted2ToneMap();
        }

        public void Init(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            this.gbuffer.SetSlot(0,
                new GBuffer.TextureSlotParam(TextureTarget.Texture2D, PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat, true, new List<ITextureParameter>
                {
                    new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear),
                    new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest),
                    new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge),
                    new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge)
                }));  // colour
            this.gbuffer.Init(this.Width, this.Height);

            program.Init(
                @"HDRExpose.vert",
                @"HDRExpose.frag",
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "in_texcoord0") 
                });

            this.gbufferCombiner = new GBufferCombiner(this.gbuffer, this.program);

            this.projection = Matrix4.CreateOrthographicOffCenter(0.0f, 1.0f, 0.0f, 1.0f, 0.001f, 10.0f);
            this.modelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);


            this.histogramTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest));
            this.histogramTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest));
            this.histogramTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge));
            this.histogramTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge));
            this.histogramTexture.UploadEmpty();

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

            this.CalculateExposure();
        }

        private void UpdateHistogram(IEnumerable<Vector3> source)
        {
            // sample counts
            Vector4[] histogram = new Vector4[HISTOGRAMWIDTH];
            int totalSamples = 0;
            float maxbucket = (float)(HISTOGRAMWIDTH - 1);
            Vector3 lumfactor = new Vector3(0.2126f, 0.7152f, 0.0722f);


            foreach (var sample in source)
            {
                histogram[((int)(sample.X * maxbucket)).ClampInclusive(0, HISTOGRAMWIDTH - 1)].X++;
                histogram[((int)(sample.Y * maxbucket)).ClampInclusive(0, HISTOGRAMWIDTH - 1)].Y++;
                histogram[((int)(sample.Z * maxbucket)).ClampInclusive(0, HISTOGRAMWIDTH - 1)].Z++;

                histogram[((int)(Vector3.Dot(sample, lumfactor) * maxbucket)).ClampInclusive(0, HISTOGRAMWIDTH - 1)].W++;

                totalSamples++;
            }

            // scale histogram so that max bucket is at the top
            int scale = totalSamples / 10;
            if (scale < 1)
            {
                scale = 1;
            }

            for (int i = 0; i < HISTOGRAMWIDTH; i++)
            {
                histogram[i] /= scale;
            }

            var texData = new byte[HISTOGRAMWIDTH * 4];

            int j = 0;
            for (int i = 0; i < HISTOGRAMWIDTH; i++)
            {
                texData[j++] = (byte)(histogram[i].X * 255.0f).ClampInclusive(0, 255);
                texData[j++] = (byte)(histogram[i].Y * 255.0f).ClampInclusive(0, 255);
                texData[j++] = (byte)(histogram[i].Z * 255.0f).ClampInclusive(0, 255);
                texData[j++] = (byte)(histogram[i].W * 255.0f).ClampInclusive(0, 255);
            }

            // update texture
            this.histogramTexture.RefreshImage(texData);
        }


        private void CalculateExposure()
        {
            // read into main memory
            var tex = this.gbuffer.GetTextureAtSlot(0);

            var leveldata = tex.GetLevelDataVector4(4);

            //((Uncharted2ToneMap)this.ToneMapper).ExposureBias = this.Exposure;

            // convert level data to luminance
            //var luminance = leveldata.Select(c=>Vector3.One - (c.Xyz * this.Exposure).Exp()).Select(c => c.X * 0.2126f + c.Y * 0.7152f + c.Z * 0.0722f).ToArray();
            //var luminance = leveldata
            //    //.Select(c => Vector3.One - (c.Xyz * this.Exposure).Exp())
            //    .Select(c => this.ToneMapper.Tonemap(c.Xyz))
            //    //.Select(c => c.Pow(1.0f/2.2f))
            //    //.Select(c => c.X * 0.3333f + c.Y * 0.3333f + c.Z * 0.3333f)
            //    .Select(c => c.X * 0.2126f + c.Y * 0.7152f + c.Z * 0.0722f)
            //    .ToArray();

            ((ReinhardToneMap)this.ToneMapper).WhiteLevel = this.WhiteLevel;

            UpdateHistogram(
                leveldata
                    .Select(c=>c.Xyz - new Vector3(this.BlackLevel))
                    //.Select(c => Vector3.One - (c * this.Exposure).Exp())
                    .Select(c => (c * -this.Exposure))
                    .Select(c => this.ToneMapper.Tonemap(c))
                    .Select(c => c.Pow(1.0f / 2.2f))
                    );

            /*
            var luminance = leveldata
                //.Select(c => Vector3.One - (c.Xyz * this.Exposure).Exp())
                .Select(c => c.Xyz)
                .Select(c => this.ToneMapper.Tonemap(c))
                .Select(c => c.Pow(1.0f / 2.2f))
                .Select(c => c.X * 0.3333f + c.Y * 0.3333f + c.Z * 0.3333f)
                //.Select(c => c.X * 0.2126f + c.Y * 0.7152f + c.Z * 0.0722f)
                .ToArray();


            // take off top and bottom 10%
            //int totalPixels = luminance.Length;
            //var averageLuminance = luminance.Skip(totalPixels / 10).Take((totalPixels * 8) / 10).Average();

            var averageLuminance = luminance.Average();

            //float targetLuminance = 0.11f;
            //float deltaLuminance = (this.TargetLuminance - averageLuminance) * 0.05f;
            float deltaLuminance = (averageLuminance - this.TargetLuminance) * 0.05f;

            this.FastExposure += deltaLuminance;
            this.SlowExposure += deltaLuminance;

            debugCol.X = averageLuminance;
            debugCol.Y = this.Exposure;
            debugCol.Z = this.FastExposure;
            debugCol.W = this.SlowExposure;
            */
            //debugCol = data[0];

            // do exposure calculation
            // lowpass
            // set final exposure
        }



        public void Render()
        {
            this.histogramTexture.Bind(TextureUnit.Texture1);
            this.gbufferCombiner.Render(projection, modelview, (sp) =>
            {
                sp.SetUniform("colTex", 0);
                sp.SetUniform("histogramTex", 1);
                sp.SetUniform("exposure", this.Exposure);
                sp.SetUniform("whitelevel", this.WhiteLevel);
                sp.SetUniform("blacklevel", this.BlackLevel);
            });
        }
    }
}
