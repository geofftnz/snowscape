using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public bool NeedThread
        {
            get { return false; }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }


        public GPUSnowTransport(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }



        public float GetHeightAt(float x, float y)
        {
            return 0f;
        }

        public void Init()
        {

        }

        public void ResetTerrain()
        {

        }

        public void ModifyTerrain()
        {

        }

        public void Load(string filename)
        {

        }

        public void Save(string filename)
        {

        }

        public float[] GetRawData()
        {
            return new float[1];
        }
    }
}
