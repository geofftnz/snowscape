using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL;
using Utils;
using System.IO;

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


        /// <summary>
        /// Terrain layers
        /// 
        /// R: ground level
        /// G: hard-packed snow
        /// B: powder
        /// A: air-suspended powder
        /// </summary>
        private Texture[] TerrainTexture = new Texture[2];
        public Texture CurrentTerrainTexture { get { return this.TerrainTexture[0]; } }

        /// <summary>
        /// Rate of windflow out of each location
        /// 
        /// R: flow up
        /// G: flow right
        /// B: flow down
        /// A: flow left
        /// </summary>
        private Texture[] FlowRateTexture = new Texture[2];
        
        /// <summary>
        /// Rate of windflow out of each location
        /// diagonal texture
        /// R: up-right
        /// G: down-right
        /// B: down-left
        /// A: up-left
        /// </summary>
        private Texture[] FlowRateTextureDiagonal = new Texture[2];

        /// <summary>
        /// Outflow due to slippage of powder
        /// 
        /// R: flow up
        /// G: flow right
        /// B: flow down
        /// A: flow left
        /// </summary>
        private Texture SlipFlowTexture;


        private GBufferShaderStep SlippageFlowStep = new GBufferShaderStep("snow-slipflow");
        private GBufferShaderStep SlippageTransportStep = new GBufferShaderStep("snow-sliptransport");
        private GBufferShaderStep TerrainCopyStep = new GBufferShaderStep("snow-terraincopy");



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


                this.FlowRateTexture[i] =
                    new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

                this.FlowRateTexture[i].UploadEmpty();

                this.FlowRateTextureDiagonal[i] =
                    new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

                this.FlowRateTextureDiagonal[i].UploadEmpty();
            }


            this.SlipFlowTexture =
                new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.SlipFlowTexture.UploadEmpty();


            SlippageFlowStep.SetOutputTexture(0, "out_Slip", this.SlipFlowTexture);
            SlippageFlowStep.Init(@"BasicQuad.vert", @"SNow_SlipOutflow.frag");

            SlippageTransportStep.SetOutputTexture(0, "out_Terrain", this.TerrainTexture[1]);
            SlippageTransportStep.Init(@"BasicQuad.vert", @"Snow_SlipTransport.frag");

            TerrainCopyStep.SetOutputTexture(0,"out_Terrain",this.TerrainTexture[0]);
            TerrainCopyStep.Init(@"BasicQuad.vert", @"Snow_TerrainCopy.frag");

        }

        public void ModifyTerrain()
        {
            // step 5 - slippage flow calc
            // in: terrain0
            // out: slip-flow
            SlippageFlowStep.Render(
                () =>
                {
                    this.TerrainTexture[0].Bind(TextureUnit.Texture0);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                    sp.SetUniform("texsize", (float)this.Width);
                    sp.SetUniform("maxdiff", 0.7f);
                    sp.SetUniform("sliprate", 0.1f);
                });

            // step 6 - slippage transport
            // in: terrain0, slip-flow
            // out: terrain1
            SlippageTransportStep.Render(
                () =>
                {
                    this.TerrainTexture[0].Bind(TextureUnit.Texture0);
                    this.SlipFlowTexture.Bind(TextureUnit.Texture1);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                    sp.SetUniform("sliptex", 1);
                    sp.SetUniform("texsize", (float)this.Width);
                });

            // terrain copy
            // in: terrain1
            // out: terrain0
            TerrainCopyStep.Render(
                () =>
                {
                    this.TerrainTexture[1].Bind(TextureUnit.Texture0);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                });


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
                this.FlowRateTexture[i].Unload();
                this.FlowRateTextureDiagonal[i].Unload();
            }
            this.SlipFlowTexture.Unload();
        }
    }
}
