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
    /// Terrain Layers:  R = hard, G = soft, B = ?, A = ?
    /// L0: terrain layer texture (read)
    /// L1: terrain layer texture (write)
    /// 
    /// P0: particle state: RG = pos, B = carrying, A = ?
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
        const int FILEMAGIC = 0x54455232;
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















        public float GetHeightAt(float x, float y)
        {
            return 0f;
        }

        public void Init()
        {
            throw new NotImplementedException();
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
