using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL;
using System.IO;

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
    public class GPUWaterErosion : ITerrainGen
    {
        const int FILEMAGIC = 0x54455230;


        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>
        /// Terrain layer texture.
        /// 
        /// R: Rock (hard).
        /// G: Soil (soft).
        /// B: Water depth.
        /// A: Material suspended in water.
        /// 
        /// managed here, copied to terrain tile and terrain global
        /// need 2 copies as we ping-pong between them each iteration
        /// </summary>
        private Texture[] TerrainTexture = new Texture[2];

        public Texture CurrentTerrainTexture
        {
            get
            {
                return this.TerrainTexture[0];
            }
        }

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


        public GPUWaterErosion(int width, int height)
        {
            this.Width = width;
            this.Height = height;

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
            }

            this.VelocityTexture =
                new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.VelocityTexture.UploadEmpty();

            this.FlowRateTexture =
                new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.FlowRateTexture.UploadEmpty();

        }

        public float GetHeightAt(float x, float y)
        {
            return 0f;
        }

        public void ModifyTerrain()
        {

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
        }

        public bool NeedThread
        {
            get { return false; }
        }


    }
}
