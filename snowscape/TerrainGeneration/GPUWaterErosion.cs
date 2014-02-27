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
    /// Takes height texture, layer depth texture
    /// 
    /// Writes to intermediate height/layer textures
    /// 
    /// Then does second pass back into original textures
    /// 
    /// Textures need to have sufficient precision. 32bit per channel? (128bit tex)
    /// 
    /// RGBA: hard-soft-water-suspended
    /// 
    /// </summary>
    public class GPUWaterErosion:ITerrainGen
    {
        // options:
        // 1: managed here and passed to terrain tile and terrain global as references.
        // 2: references here, but passed in from terrain globals. May need to apply sampler to terrain tile.
        // 3: managed here, copied to terrain tile and terrain global
        private Texture[] TerrainTexture = new Texture[2];


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
