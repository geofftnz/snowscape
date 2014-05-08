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
    ///   Calculate particle potential as new carrying capacity - old carrying capacity.
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


        // particle VBOs for accumulation rendering

        // Step 1: Determine particle velocities
        private GBufferShaderStep ComputeVelocityStep = new GBufferShaderStep("gpupe-1-velocity");
        
        // Step 2: Accumulate erosion/sediment
        private VBO ParticleVertexVBO = new VBO("particle-vertex");
        private VBO ParticleIndexVBO = new VBO("particle-index", BufferTarget.ElementArrayBuffer);
        private ShaderProgram ErosionAccumulationProgram = new ShaderProgram("erosion-accumulation");
        private GBuffer ErosionAccumulationGBuffer = new GBuffer("erosion-accumulation");

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
                    new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

                this.ParticleStateTexture[i].UploadEmpty();

                this.VelocityTexture[i] =
                    new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
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
            ///   Calculate particle potential as new carrying capacity - old carrying capacity.
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


        }

        public void Unload()
        {
            throw new NotImplementedException();
        }

        public void ResetTerrain()
        {
            throw new NotImplementedException();
        }

        public void ModifyTerrain()
        {
            throw new NotImplementedException();
        }

        public void Load(string filename)
        {
            throw new NotImplementedException();
        }

        public void Save(string filename)
        {
            throw new NotImplementedException();
        }

        public float[] GetRawData()
        {
            throw new NotImplementedException();
        }
    }
}
