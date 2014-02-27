using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTKExtensions;

namespace TerrainGeneration
{

    /// <summary>
    /// GPU-based water erosion
    /// 
    /// Uses layer depth texture
    /// 
    /// Writes to intermediate height/layer textures
    /// 
    /// Then does second pass back into original textures
    /// 
    /// Textures need to have sufficient precision. 32bit per channel? (128bit tex)
    /// 
    /// RGBA: hard-soft-water-suspended
    /// 
    /// Algorithm (from MDH07 Fast Hydraulic Erosion Simulation and Visualization on GPU http://www-evasion.imag.fr/Publications/2007/MDH07/ and my own extensions to their model)
    /// 
    /// L1 = layer texture being written to
    /// L0 = layer texture being read from
    /// F = flow rate texture
    /// V = velocity texture
    /// 
    /// GBuffer step. Inputs: L0, Outputs: F
    /// - Compute height of pos + neighbours as L0.r + L0.g + L0.b
    /// - F.rgba = outflow from L0
    /// ----- 
    /// GBuffer step. Inputs: F, Outputs: V
    /// - V = velocity based on flow gradient of F
    /// -----
    /// GBuffer step. Inputs L0, F, V. Outputs L1
    /// - compute carrying capacity based on flow rate and ground gradient
    /// - erode/deposit based on carrying capacity and sediment (writes to L1.r, L1.g, L1.a)
    /// - evaporate (constant based on surface area, not volume) (writes to L1.g, L1.a, L1.b)
    /// - add water from rain (writes to L1.b)
    /// -----
    /// GBuffer step. Inputs L1, V. Outputs L0
    /// - transport sediment by interpolated lookup of L1.a = L0[p - normalize(V)].a (writes to L1.a)
    /// -----
    /// 
    /// </summary>
    public class GPUWaterErosion:ITerrainGen
    {
        // managed here, copied to terrain tile and terrain global
        // need 2 copies as we ping-pong between them each iteration

        /// <summary>
        /// Terrain layer texture.
        /// 
        /// R: Rock (hard).
        /// G: Soil (soft).
        /// B: Water depth.
        /// A: Material suspended in water.
        /// </summary>
        private Texture[] TerrainTexture = new Texture[2];

        /// <summary>
        /// Velocity of water over terrain. Used for erosion potential.
        /// RG
        /// </summary>
        private Texture VelocityTexture;

        /// <summary>
        /// Rate of flow out of each location
        /// 
        /// R: flow up
        /// G: flow right
        /// B: flow down
        /// A: flow left
        /// </summary>
        private Texture FlowRateTexture;


        public void ModifyTerrain()
        {
            throw new NotImplementedException();
        }

        public void Load(string filename)
        {
            //throw new NotImplementedException();
        }

        public void Save(string filename)
        {
            //throw new NotImplementedException();
        }
    }
}
