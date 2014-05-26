using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL;
using Utils;
using System.IO;
using OpenTK;

namespace TerrainGeneration
{

    /// <summary>
    /// Snow transport on the GPU.
    /// 
    /// Snow moves in the following ways:
    /// 
    /// - Compaction of powder
    /// - Downhill flow of powder
    /// - Transport by wind
    /// - Evaporation/melting
    /// 
    /// Terrain Layer texture:
    /// 
    /// R: base ground height
    /// G: packed snow thickness
    /// B: powder thickness
    /// A: powder suspended in air
    /// 
    /// Outflow texture(s): as per water erosion
    /// 
    /// Velocity texture: represents wind strength and direction over this cell
    /// 
    /// 
    /// </summary>
    public class GPUSnowTransport : ITerrainGen
    {
        const int FILEMAGIC = 0x54455231;  // different format for pass 2

        public bool NeedThread { get { return false; } }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public int ParticleTexWidth { get; set; }
        public int ParticleTexHeight { get; set; }

        /// <summary>
        /// Terrain layers
        /// 
        /// R: hard rock
        /// G: soft soil
        /// B: hard-packed snow
        /// A: powder
        /// </summary>
        private Texture[] TerrainTexture = new Texture[2];
        public Texture CurrentTerrainTexture { get { return this.TerrainTexture[0]; } }

        /// <summary>
        /// Outflow due to powder slipping beyond its angle of repose (hdiff = 0.62) 
        /// Ortho + Diagonal
        /// 
        /// RGBA = N S W E 
        /// RGBA = NW NE SW SE
        /// </summary>
        private Texture[] SlipFlowTexture = new Texture[2];


        /// <summary>
        /// Particle state texture.
        /// 
        /// RG: xy of particle, 0-width/height in terrain space.
        /// B: carrying amount
        /// </summary>
        private Texture[] ParticleStateTexture = new Texture[2];
        public Texture CurrentParticleTexture { get { return this.ParticleStateTexture[0]; } }

        /// <summary>
        /// Particle velocity texture.
        /// 
        /// RG: velocity
        /// B: new carrying capacity
        /// </summary>
        private Texture[] VelocityTexture = new Texture[2];

        /// <summary>
        /// terrain erosion accumulation: R = particle count, G = total erosion potential, B = total material deposit, A = total lofted powder
        /// </summary>
        private Texture ErosionAccumulationTexture;
        public Texture ErosionTex { get { return ErosionAccumulationTexture; } }



        private GBufferShaderStep SnowfallStep = new GBufferShaderStep("snow-snowfall");
        private GBufferShaderStep SlippageFlowStep = new GBufferShaderStep("snow-slipflow");
        private GBufferShaderStep SlippageTransportStep = new GBufferShaderStep("snow-sliptransport");

        private VBO ParticleVertexVBO = new VBO("particle-vertex");
        private VBO ParticleIndexVBO = new VBO("particle-index", BufferTarget.ElementArrayBuffer);
        private GBufferShaderStep ErosionAccumulationStep = new GBufferShaderStep("snow-accumulation");

        // Step 3: Update terrain layers
        private GBufferShaderStep UpdateLayersStep = new GBufferShaderStep("snow-updatelayers");

        // Step 4: Update particle state
        private GBufferShaderStep UpdateParticlesStep = new GBufferShaderStep("snow-updateparticles");



        //private GBufferShaderStep TerrainCopyStep = new GBufferShaderStep("snow-terraincopy");

        private ParameterCollection parameters = new ParameterCollection();
        public ParameterCollection Parameters { get { return parameters; } }

        private const string P_SNOWRATE = "snow-fallrate";
        private const string P_SLIPTHRESHOLD = "snow-slipthreshold";
        private const string P_SLIPRATE = "snow-sliprate";


        public GPUSnowTransport(int width, int height, int particleTexWidth, int particleTexHeight)
        {
            this.Width = width;
            this.Height = height;
            this.ParticleTexWidth = particleTexWidth;
            this.ParticleTexHeight = particleTexHeight;

            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_SNOWRATE, 0.001f, 0.0f, 0.1f));
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_SLIPTHRESHOLD, 0.62f, 0.0f, 4.0f, 0.001f));
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_SLIPRATE, 0.001f, 0.0f, 0.1f, 0.001f));
        }



        public float GetHeightAt(float x, float y)
        {
            return 0f;
        }

        public void Init()
        {
            // setup textures
            for (int i = 0; i < 2; i++)
            {
                this.TerrainTexture[i] =
                    new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

                this.TerrainTexture[i].UploadEmpty();

                this.ParticleStateTexture[i] =
                    new Texture(this.ParticleTexWidth, this.ParticleTexHeight, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

                this.ParticleStateTexture[i].UploadEmpty();

                this.VelocityTexture[i] =
                    new Texture(this.ParticleTexWidth, this.ParticleTexHeight, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

                this.VelocityTexture[i].UploadEmpty();

                this.SlipFlowTexture[i] =
                    new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

                this.SlipFlowTexture[i].UploadEmpty();
            }


            this.ErosionAccumulationTexture = new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.ErosionAccumulationTexture.UploadEmpty();

            // init particle vbos
            InitParticlesVBOs();

            // random start
            RandomizeParticles(this.ParticleStateTexture[0]);


            SnowfallStep.SetOutputTexture(0, "out_Terrain", this.TerrainTexture[1]);
            SnowfallStep.Init(@"BasicQuad.vert", @"SnowTransport.glsl|SnowFall");

            SlippageFlowStep.SetOutputTexture(0, "out_SlipO", this.SlipFlowTexture[0]);
            SlippageFlowStep.SetOutputTexture(1, "out_SlipD", this.SlipFlowTexture[1]);
            SlippageFlowStep.Init(@"BasicQuad.vert", @"SoftSlip.glsl|SnowOutflow");

            SlippageTransportStep.SetOutputTexture(0, "out_Terrain", this.TerrainTexture[0]);
            SlippageTransportStep.Init(@"BasicQuad.vert", @"SoftSlip.glsl|SnowTransport");

            //TerrainCopyStep.SetOutputTexture(0, "out_Terrain", this.TerrainTexture[0]);
            //TerrainCopyStep.Init(@"BasicQuad.vert", @"Snow_TerrainCopy.frag");

        }

        public void ModifyTerrain()
        {


            SnowfallStep.Render(
                () =>
                {
                    this.TerrainTexture[0].Bind(TextureUnit.Texture0);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                    sp.SetUniform("snowfallrate", (float)this.Parameters[P_SNOWRATE].GetValue());
                });
            
            // step 5 - slippage flow calc
            // in: terrain0
            // out: slip-flow
            SlippageFlowStep.Render(
                () =>
                {
                    this.TerrainTexture[1].Bind(TextureUnit.Texture0);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                    sp.SetUniform("texsize", (float)this.Width);
                    sp.SetUniform("maxdiff", (float)this.Parameters[P_SLIPTHRESHOLD].GetValue());
                    sp.SetUniform("sliprate", (float)this.Parameters[P_SLIPRATE].GetValue());

                    //sp.SetUniform("threshold", (float)this.Parameters[P_SLIPRATE].GetValue());
                    //sp.SetUniform("minslip", (float)this.Parameters[P_SLIPRATE].GetValue());
                    //sp.SetUniform("maxslip", (float)this.Parameters[P_SLIPRATE].GetValue());
                });

            // step 6 - slippage transport
            // in: terrain0, slip-flow
            // out: terrain1
            SlippageTransportStep.Render(
                () =>
                {
                    this.TerrainTexture[1].Bind(TextureUnit.Texture0);
                    this.SlipFlowTexture[0].Bind(TextureUnit.Texture1);
                    this.SlipFlowTexture[1].Bind(TextureUnit.Texture2);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                    sp.SetUniform("flowOtex", 1);
                    sp.SetUniform("flowDtex", 2);
                    sp.SetUniform("texsize", (float)this.Width);
                });

            // terrain copy
            // in: terrain1
            // out: terrain0
            //TerrainCopyStep.Render(
            //    () =>
            //    {
            //        this.TerrainTexture[1].Bind(TextureUnit.Texture0);
            //    },
            //    (sp) =>
            //    {
            //        sp.SetUniform("terraintex", 0);
            //    });


        }




        private void UploadTerrain(float[] data)
        {
            this.TerrainTexture[0].Upload(data);
        }

        private float[] DownloadTerrain()
        {
            return this.TerrainTexture[0].GetLevelDataFloat(0);
        }


        public void InitFromPass1(ITerrainGen src)
        {
            var srcdata = src.GetRawData();
            float[] destdata = new float[this.Width * this.Height * 4];

            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                int j = i * 4;
                destdata[j + 0] = srcdata[j + 0] + srcdata[j + 1]; // ground = hard + soft
                destdata[j + 1] = 0f;
                destdata[j + 2] = 0f;
                destdata[j + 3] = 0f;
            });

            this.UploadTerrain(destdata);
        }

        /// <summary>
        /// Removes all snow
        /// </summary>
        public void ResetTerrain()
        {
            var data = DownloadTerrain();

            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                int j = i * 4;
                data[j + 1] = 0f;
                data[j + 2] = 0f;
                data[j + 3] = 0f;
            });

            UploadTerrain(data);

        }


        public void Load(string filename)
        {
            var data = new float[this.Width * this.Height * 4];

            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None, 256 * 1024))
            {
                using (var sr = new BinaryReader(fs))
                {
                    int magic = sr.ReadInt32();
                    if (magic != FILEMAGIC)
                    {
                        throw new Exception("Not a terrain file");
                    }

                    int w, h;

                    w = sr.ReadInt32();
                    h = sr.ReadInt32();

                    if (w != this.Width || h != this.Height)
                    {
                        // TODO: handle size changes
                        throw new Exception(string.Format("Terrain size {0}x{1} did not match generator size {2}x{3}", w, h, this.Width, this.Height));
                    }

                    for (int i = 0; i < w * h; i++)
                    {
                        data[i * 4 + 0] = sr.ReadSingle();
                        data[i * 4 + 1] = sr.ReadSingle();
                        data[i * 4 + 2] = sr.ReadSingle();
                        data[i * 4 + 3] = 0f; sr.ReadSingle();
                    }

                    sr.Close();
                }
                fs.Close();
            }

            UploadTerrain(data);

        }

        public void Save(string filename)
        {
            var data = this.DownloadTerrain();

            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 256 * 1024))
            {
                using (var sw = new BinaryWriter(fs))
                {
                    sw.Write(FILEMAGIC);
                    sw.Write(this.Width);
                    sw.Write(this.Height);

                    for (int i = 0; i < this.Width * this.Height; i++)
                    {
                        sw.Write(data[i * 4 + 0]);
                        sw.Write(data[i * 4 + 1]);
                        sw.Write(data[i * 4 + 2]);
                        sw.Write(data[i * 4 + 3]);
                    }

                    sw.Close();
                }
                fs.Close();
            }
        }

        public float[] GetRawData()
        {
            return DownloadTerrain();
        }


        public void Unload()
        {
            for (int i = 0; i < 2; i++)
            {
                this.TerrainTexture[i].Unload();
                this.ParticleStateTexture[i].Unload();
                this.VelocityTexture[i].Unload();
                this.SlipFlowTexture[i].Unload();
            }
            this.ErosionAccumulationTexture.Unload();
        }


        public IEnumerable<IParameter> GetParameters()
        {
            return this.Parameters;
        }

        private void InitParticlesVBOs()
        {
            Vector3[] vertex = new Vector3[this.ParticleTexWidth * this.ParticleTexHeight];

            ParallelHelper.For2D(this.ParticleTexWidth, this.ParticleTexHeight, (x, y, i) =>
            {
                vertex[i] = new Vector3((float)x / (float)this.ParticleTexWidth, (float)y / (float)this.ParticleTexHeight, 0f);
            });

            this.ParticleVertexVBO.SetData(vertex);

            uint[] index = new uint[this.ParticleTexWidth * this.ParticleTexHeight];

            for (int i = 0; i < this.ParticleTexWidth * this.ParticleTexHeight; i++)
            {
                index[i] = (uint)i;
            }

            this.ParticleIndexVBO.SetData(index);
        }

        private void RandomizeParticles(Texture destination)
        {
            float[] data = new float[this.ParticleTexWidth * this.ParticleTexHeight * 4];
            var r = new Random();

            for (int i = 0; i < this.ParticleTexWidth * this.ParticleTexHeight; i++)
            {
                data[i * 4 + 0] = (float)r.NextDouble();  // x
                data[i * 4 + 1] = (float)r.NextDouble();  // y 
                data[i * 4 + 2] = 0f;    // carrying nothing
                data[i * 4 + 3] = (float)r.NextDouble();  // particle life
            }

            destination.Upload(data);
        }


    }
}
