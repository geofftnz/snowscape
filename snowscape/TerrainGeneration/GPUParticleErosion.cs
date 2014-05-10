using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL;
using System.IO;
using Utils;

namespace TerrainGeneration
{

    /// <summary>
    /// GPU-based particle erosion.
    /// 
    /// An attempt to get the original CPU-based particle erosion working on the GPU
    /// 
    /// Needs the following textures/buffers
    /// 
    /// Terrain Layers:  R = hard, G = soft, B = water depth, A = ?
    /// L0: terrain layer texture (read)
    /// L1: terrain layer texture (write)
    /// 
    /// P0: particle state: RG = pos, B = carrying, A = ? (maybe death flag or carrying capacity)
    /// P1: particle state: RG = pos, B = carrying, A = ?
    /// NOTE: pos is normalized 0-1
    /// 
    /// V0: particle velocity: RG = vel, B = new carrying capacity, A = death flag
    /// V1: particle velocity: RG = vel, B = new carrying capacity, A = death flag
    /// 
    /// E: terrain erosion accumulation: R = particle count, G = total erosion potential, B = total material deposit
    /// 
    /// 
    /// Step 1: Determine particle velocities
    ///  - Render as quad over particles
    ///  - Input: L0, P0, V1
    ///  - Output: V0
    ///  Uses slope information from L0 at position P0 to calculate acceleration of particle
    ///  Takes velocity from V1.rg, applies acceleration, writes to V0.rg
    ///  Calculates new carrying capacity and writes to V0.b 
    ///  If slope information indicates particle is stuck, zero/reduce new carrying capacity, set death flag V0.a.
    ///  
    /// Step 2: Accumulate erosion/sediment
    ///  - Render as particles over layer
    ///  - Input: P0, V0
    ///  - Output: E
    ///   Vertex shader sets particle position in E based on P0.
    ///   Calculate particle potential as (new carrying capacity - carrying amount).
    ///   Writes R:1 G:potential B:deposit
    ///   Blend mode: add
    ///  
    /// Step 3: Update terrain layers
    ///  - Render as quad over layer
    ///  - Input: L0, E
    ///  - Output: L1
    ///  Adds deposit amount E.b to soft L0.g
    ///  Subtracts material from soft, then hard.
    ///  Modifies water depth from particle count (E.r).
    ///  
    /// Step 4: Update particle state
    ///  - Render as quad over particles
    ///  - Input: P0, V0, E, L0
    ///  - Output: P1
    ///  Replicate particle potential calc from Step 2 to get deposit/erode potentials.
    ///  Subtract deposit amount from carrying amount P0.b, write to P1.b
    ///  Apply same calculation as step 3 to determine how much soft/hard is being eroded from L0(P0.rg).
    ///  Add material carried based on particle share of total V0.b / E(P0.rg).g -> P1.b
    ///  Calculate new particle position by intersecting ray P0.rg->V0.rg against 
    ///    cell boundaries. Add small offset to avoid boundary problems. Writes to P1.rg
    ///  If death flag indicates particle recycle, init particle at random position.
    ///  
    /// Step 5: Slip flow
    /// in: L1
    /// out: flow
    /// 
    /// Step 6: Slip transport
    /// in: L1,flow
    /// out: L0    (avoids L0/L1 switch)
    /// 
    /// 
    /// Final:
    /// Copy V0->V1
    /// Switch P0/P1
    ///  
    /// 
    /// Visualisation:
    /// - Render as lines over view gbuffer
    /// - Input: P0,P1,Heightmap
    /// - Use P0.rg->P1.rg as the line.
    /// 
    /// 
    /// 
    /// 
    /// </summary>
    public class GPUParticleErosion : ITerrainGen
    {
        const int FILEMAGIC = 0x54455230;
        public bool NeedThread { get { return false; } }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int ParticleTexWidth { get; set; }
        public int ParticleTexHeight { get; set; }

        public GPUParticleErosion(int width, int height, int particleTexWidth, int particleTexHeight)
        {
            this.Width = width;
            this.Height = height;
            this.ParticleTexWidth = particleTexWidth;
            this.ParticleTexHeight = particleTexHeight;
        }


        /// <summary>
        /// Terrain layer texture.
        /// 
        /// R: Rock (hard).
        /// G: Soil (soft).
        /// B: Water depth. (for vis only)
        /// A: nothing
        /// 
        /// managed here, copied to terrain tile and terrain global
        /// need 2 copies as we ping-pong between them each iteration
        /// </summary>
        private Texture[] TerrainTexture = new Texture[2];
        public Texture CurrentTerrainTexture { get { return this.TerrainTexture[0]; } }
        public Texture TempTerrainTexture { get { return this.TerrainTexture[1]; } }

        /// <summary>
        /// Particle state texture.
        /// 
        /// RG: xy of particle, 0-width/height in terrain space.
        /// B: carrying amount
        /// </summary>
        private Texture[] ParticleStateTexture = new Texture[2];

        /// <summary>
        /// Particle velocity texture.
        /// 
        /// RG: velocity
        /// B: new carrying capacity
        /// A: death flag
        /// </summary>
        private Texture[] VelocityTexture = new Texture[2];


        /// <summary>
        /// terrain erosion accumulation: R = particle count, G = total erosion potential, B = total material deposit
        /// </summary>
        private Texture ErosionAccumulationTexture;
        public Texture ErosionTex { get { return ErosionAccumulationTexture; } }


        // particle VBOs for accumulation rendering

        // Step 1: Determine particle velocities
        private GBufferShaderStep ComputeVelocityStep = new GBufferShaderStep("gpupe-1-velocity");

        // Step 2: Accumulate erosion/sediment
        private VBO ParticleVertexVBO = new VBO("particle-vertex");
        private VBO ParticleIndexVBO = new VBO("particle-index", BufferTarget.ElementArrayBuffer);
        private GBufferShaderStep ErosionAccumulationStep = new GBufferShaderStep("gpupe-2-accumulation");
        //private ShaderProgram ErosionAccumulationProgram = new ShaderProgram("erosion-accumulation");
        //private GBuffer ErosionAccumulationGBuffer = new GBuffer("erosion-accumulation");

        // Step 3: Update terrain layers
        private GBufferShaderStep UpdateLayersStep = new GBufferShaderStep("gpupe-3-updatelayers");

        // Step 4: Update particle state
        private GBufferShaderStep UpdateParticlesStep = new GBufferShaderStep("gpupe-4-updateparticles");

        // Step 5,6: Slip 
        private GBufferShaderStep SlippageFlowStep = new GBufferShaderStep("erosion-slipflow");
        private GBufferShaderStep SlippageTransportStep = new GBufferShaderStep("erosion-sliptransport");


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

            // Step 1: Determine particle velocities
            //  - Render as quad over particles
            //  - Input: L0, P0, V1
            //  - Output: V0
            //  Uses slope information from L0 at position P0 to calculate acceleration of particle
            //  Takes velocity from V1.rg, applies acceleration, writes to V0.rg
            //  Calculates new carrying capacity and writes to V0.b 
            //  If slope information indicates particle is stuck, zero/reduce new carrying capacity, set death flag V0.a.
            ComputeVelocityStep.SetOutputTexture(0, "out_Velocity", this.VelocityTexture[0]);
            ComputeVelocityStep.Init(@"BasicQuad.vert", @"ParticleErosion.glsl|ComputeVelocity");



            // Step 2: Accumulate erosion/sediment
            //  - Render as particles over layer
            //  - Input: P0, V0
            //  - Output: E
            //   Vertex shader sets particle position in E based on P0.
            //   Calculate particle potential as (new carrying capacity - carrying amount).
            //   Writes R:1 G:potential B:deposit
            //   Blend mode: add
            ErosionAccumulationStep.SetOutputTexture(0, "out_Erosion", this.ErosionAccumulationTexture);
            ErosionAccumulationStep.Init(@"ParticleErosion.glsl|ErosionVertex", @"ParticleErosion.glsl|Erosion");




            // Step 3: Update terrain layers
            //  - Render as quad over layer
            //  - Input: L0, E
            //  - Output: L1
            //  Adds deposit amount E.b to soft L0.g
            //  Subtracts material from soft, then hard.
            //  Modifies water depth from particle count (E.r).
            //  
            // Step 4: Update particle state
            //  - Render as quad over particles
            //  - Input: P0, V0, E, L0
            //  - Output: P1
            //  Replicate particle potential calc from Step 2 to get deposit/erode potentials.
            //  Subtract deposit amount from carrying amount P0.b, write to P1.b
            //  Apply same calculation as step 3 to determine how much soft/hard is being eroded from L0(P0.rg).
            //  Add material carried based on particle share of total V0.b / E(P0.rg).g -> P1.b
            //  Calculate new particle position by intersecting ray P0.rg->V0.rg against 
            //    cell boundaries. Add small offset to avoid boundary problems. Writes to P1.rg
            //  If death flag indicates particle recycle, init particle at random position.
            //  
            // Step 5: Slip flow
            // in: L1
            // out: flow
            // 
            // Step 6: Slip transport
            // in: L1,flow
            // out: L0    (avoids L0/L1 switch)


        }


        public void ModifyTerrain()
        {
            float deltatime = 1.0f;

            ComputeVelocityStep.Render(
                () =>
                {
                    this.TerrainTexture[0].Bind(TextureUnit.Texture0);
                    this.ParticleStateTexture[0].Bind(TextureUnit.Texture1);
                    this.VelocityTexture[1].Bind(TextureUnit.Texture2);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                    sp.SetUniform("particletex", 1);
                    sp.SetUniform("velocitytex", 2);
                    sp.SetUniform("texsize", (float)this.ParticleTexWidth);
                    sp.SetUniform("vdecay", 0.5f);
                    sp.SetUniform("vadd", 0.5f);
                    sp.SetUniform("speedCarryingCoefficient", 1.0f);
                });

            // accumulate erosion
            //ErosionAccumulationStep.ClearColourBuffer(0, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            ErosionAccumulationStep.Render(
                () =>
                {
                    this.ParticleStateTexture[0].Bind(TextureUnit.Texture0);
                    this.VelocityTexture[1].Bind(TextureUnit.Texture1);
                },
                (sp) =>
                {
                    sp.SetUniform("particletex", 0);
                    sp.SetUniform("velocitytex", 1);
                    sp.SetUniform("deltatime", deltatime);
                    sp.SetUniform("depositRate", 0.1f);
                    sp.SetUniform("erosionRate", 0.1f);
                },
                () =>
                {
                    GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
                    GL.Clear(ClearBufferMask.ColorBufferBit);
                    GL.Enable(EnableCap.Blend);
                    GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
                    this.ParticleVertexVBO.Bind(this.ErosionAccumulationStep.ShaderVariableLocation("vertex"));
                    this.ParticleIndexVBO.Bind();
                    GL.DrawElements(BeginMode.Points, this.ParticleIndexVBO.Length, DrawElementsType.UnsignedInt, 0);
                    GL.Disable(EnableCap.Blend);
                }
            );



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

        public void Unload()
        {
            throw new NotImplementedException();
        }


        private void UploadTerrain(float[] data)
        {
            this.TerrainTexture[0].Upload(data);
        }

        private float[] DownloadTerrain()
        {
            return this.TerrainTexture[0].GetLevelDataFloat(0);
        }

        public float[] GetRawData()
        {
            return DownloadTerrain();
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
                        data[i * 4 + 2] = 0f; sr.ReadSingle();
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
                        sw.Write(0f);
                        sw.Write(0f);
                    }

                    sw.Close();
                }
                fs.Close();
            }
        }


        public void ResetTerrain()
        {
            Snowscape.TerrainStorage.Terrain terrain = new Snowscape.TerrainStorage.Terrain(this.Width, this.Height);

            terrain.Clear(0.0f);
            terrain.AddSimplexNoise(4, 0.16f / (float)this.Width, 300.0f, h => h, h => Math.Abs(h));
            terrain.AddSimplexNoise(14, 1.0f / (float)this.Width, 200.0f, h => Math.Abs(h), h => h);
            //terrain.AddSimplexNoise(6, 19.0f / (float)this.Width, 20.0f, h => h*h, h => h);
            terrain.AddLooseMaterial(1.0f);
            terrain.SetBaseLevel();

            var data = new float[this.Width * this.Height * 4];

            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                data[i * 4 + 0] = terrain[i].Hard;
                data[i * 4 + 1] = terrain[i].Loose;
                data[i * 4 + 2] = 0.0f;  // water
                data[i * 4 + 3] = 0.0f;
            });

            UploadTerrain(data);

        }

        private void RandomizeParticles(Texture destination)
        {
            float[] data = new float[this.ParticleTexWidth * this.ParticleTexHeight * 4];
            var r = new Random();

            for (int i = 0; i < this.ParticleTexWidth * this.ParticleTexHeight; i++)
            {
                data[i * 4 + 0] = (float)r.NextDouble();
                data[i * 4 + 1] = (float)r.NextDouble();
                data[i * 4 + 2] = 0f;
                data[i * 4 + 3] = 0f;
            }

            destination.Upload(data);
        }



    }
}
