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

        public Texture CurrentTerrainTexture { get { return this.TerrainTexture[0]; } }
        public Texture TempTerrainTexture { get { return this.TerrainTexture[1]; } }

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
        private Texture[] FlowRateTexture = new Texture[2];

        public Texture FlowRateTex { get { return FlowRateTexture[0]; } }  // for visualisation
        public Texture VelocityTex { get { return VelocityTexture; } }  // for visualisation

        public Texture VisTex { get; set; }

        // Shader steps
        private GBufferShaderStep ComputeOutflowStep = new GBufferShaderStep("erosion-outflow");
        private GBufferShaderStep ComputeVelocityStep = new GBufferShaderStep("erosion-velocity");
        private GBufferShaderStep UpdateLayersStep = new GBufferShaderStep("erosion-updatelayers");
        private GBufferShaderStep SedimentTransportStep = new GBufferShaderStep("erosion-transport");



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
                

                this.FlowRateTexture[i] =
                    new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

                this.FlowRateTexture[i].UploadEmpty();
            
            }

            this.VelocityTexture =
                new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.VelocityTexture.UploadEmpty();

            this.VisTex =
                new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.VisTex.UploadEmpty();


            // setup steps
            ComputeOutflowStep.SetOutputTexture(0, "out_Flow", this.FlowRateTexture[1]);
            ComputeOutflowStep.Init(@"../../../Resources/Shaders/BasicQuad.vert".Load(), @"../../../Resources/Shaders/Erosion_1Outflow.frag".Load());

            ComputeVelocityStep.SetOutputTexture(0, "out_Velocity", this.VelocityTexture);
            ComputeVelocityStep.Init(@"../../../Resources/Shaders/BasicQuad.vert".Load(), @"../../../Resources/Shaders/Erosion_2Velocity.frag".Load());

            UpdateLayersStep.SetOutputTexture(0, "out_Terrain", this.TerrainTexture[1]);
            UpdateLayersStep.SetOutputTexture(1, "out_Vis", this.VisTex);
            UpdateLayersStep.Init(@"../../../Resources/Shaders/BasicQuad.vert".Load(), @"../../../Resources/Shaders/Erosion_3UpdateLayers.frag".Load());

            SedimentTransportStep.SetOutputTexture(0, "out_Terrain", this.TerrainTexture[0]);
            SedimentTransportStep.SetOutputTexture(1, "out_Flow", this.FlowRateTexture[0]);
            SedimentTransportStep.Init(@"../../../Resources/Shaders/BasicQuad.vert".Load(), @"../../../Resources/Shaders/Erosion_4Transport.frag".Load());
        }

        public float GetHeightAt(float x, float y)
        {
            return 0f;
        }

        public void ModifyTerrain()
        {

            // step 1 - compute flows

            ComputeOutflowStep.Render(
                    () =>
                    {
                        this.TerrainTexture[0].Bind(TextureUnit.Texture0);
                        this.FlowRateTexture[0].Bind(TextureUnit.Texture1);
                    },
                    (sp) =>
                    {
                        sp.SetUniform("terraintex", 0);
                        sp.SetUniform("flowtex", 1);
                        sp.SetUniform("texsize", (float)this.Width);
                        sp.SetUniform("flowRate", 0.5f);  // todo: hoist parameter
                        sp.SetUniform("flowLowpass", 0.5f);  // todo: hoist parameter
                    }
                );

            // step 2 - compute velocity
            ComputeVelocityStep.Render(
                () =>
                {
                    this.FlowRateTexture[1].Bind(TextureUnit.Texture0);
                },
                (sp) =>
                {
                    sp.SetUniform("flowtex", 0);
                    sp.SetUniform("texsize", (float)this.Width);
                });

            // step 3 - update layers
            UpdateLayersStep.Render(
                () =>
                {
                    this.TerrainTexture[0].Bind(TextureUnit.Texture0);
                    this.FlowRateTexture[1].Bind(TextureUnit.Texture1);
                    this.VelocityTexture.Bind(TextureUnit.Texture2);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                    sp.SetUniform("flowtex", 1);
                    sp.SetUniform("velocitytex", 2);
                    sp.SetUniform("texsize", (float)this.Width);
                    sp.SetUniform("capacitybias", 0.0f);
                    sp.SetUniform("capacityscale", 5.0f);
                    sp.SetUniform("rockerodability", 0.5f);
                    sp.SetUniform("erosionfactor", 0.1f);
                    sp.SetUniform("depositfactor", 0.1f);
                    sp.SetUniform("evaporationfactor", 1.0f);
                });


            // step 4 - sediment transport
            SedimentTransportStep.Render(
                () =>
                {
                    this.TerrainTexture[1].Bind(TextureUnit.Texture0);
                    this.FlowRateTexture[1].Bind(TextureUnit.Texture1);
                    this.VelocityTexture.Bind(TextureUnit.Texture2);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                    sp.SetUniform("flowtex", 1);
                    sp.SetUniform("velocitytex", 2);
                    sp.SetUniform("texsize", (float)this.Width);
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
            terrain.AddSimplexNoise(6, 0.3333f / (float)this.Width, 50.0f, h => h, h => Math.Abs(h));
            terrain.AddSimplexNoise(14, 0.37f / (float)this.Width, 300.0f, h => Math.Abs(h), h => h);
            terrain.AddLooseMaterial(15.0f);
            terrain.SetBaseLevel();

            var data = new float[this.Width * this.Height * 4];

            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                data[i * 4 + 0] = terrain[i].Hard;
                data[i * 4 + 1] = terrain[i].Loose;
                data[i * 4 + 2] = 1.0f;  // water
                data[i * 4 + 3] = 0.0f;
            });

            UploadTerrain(data);

        }

        public bool NeedThread
        {
            get { return false; }
        }


    }
}
