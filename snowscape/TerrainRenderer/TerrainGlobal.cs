﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL4;
using Utils;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainRenderer
{
    /// <summary>
    /// TerrainGlobal - stores global state of terrain
    /// 
    /// Knows about:
    /// - height map of entire terrain, either full scale or downsampled to fit within texture limits.
    /// - shadow height above each sample
    /// - amount of sky visible from each sample (for AO)
    /// - min / max height of terrain, to set exit conditions of rays
    /// </summary>
    public class TerrainGlobal : GameComponentBase, IListTextures
    {

        public const int DetailRes = 1024;

        /// <summary>
        /// Height - single component float32
        /// </summary>
        public Texture HeightTexture { get; private set; }

        /// <summary>
        /// Shade - double-component float16
        /// R: Height of shadow plane.
        /// G: Proportion of sky visible.
        /// </summary>
        public Texture ShadeTexture { get; private set; }

        /// <summary>
        /// Indirect Illumination Texture - float16
        /// Represents the amount of indirect illumination from lit parts of the terrain.
        /// </summary>
        public Texture IndirectIlluminationTexture { get; private set; }

        /// <summary>
        /// Detail texture - RGBA byte
        /// </summary>
        public Texture TerrainDetailTexture { get; private set; }


        // TODO: Lose these
        public float MinHeight { get; private set; }
        public float MaxHeight { get; private set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public TerrainGlobal(int width, int height)
            : base()
        {
            this.Width = width;
            this.Height = height;
            this.InitTextures();
            this.Loading += TerrainGlobal_Loading;
            this.Unloading += TerrainGlobal_Unloading;
        }

        public void InitTextures()
        {
            // setup textures
            this.HeightTexture =
                new Texture("Height", this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));


            this.ShadeTexture =
                new Texture("Shade", this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rg16f, PixelFormat.Rg, PixelType.HalfFloat)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));


            this.IndirectIlluminationTexture =
                new Texture("Indirect", this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.R16f, PixelFormat.Red, PixelType.HalfFloat)
                //new Texture(this.Width, this.Height,TextureTarget.Texture2D, PixelInternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.TerrainDetailTexture = new Texture("TerrainDetail", DetailRes, DetailRes, TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);
            this.TerrainDetailTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat));
            this.TerrainDetailTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));
            this.TerrainDetailTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
            this.TerrainDetailTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear));


        }

        public IEnumerable<Texture> Textures()
        {
            yield return this.HeightTexture;
            yield return this.ShadeTexture;
            yield return this.IndirectIlluminationTexture;
            yield return this.TerrainDetailTexture;
        }

        void TerrainGlobal_Loading(object sender, EventArgs e)
        {
            this.HeightTexture.UploadEmpty();
            this.ShadeTexture.UploadEmpty();
            this.IndirectIlluminationTexture.UploadEmpty();

            GenerateDetailTexture();
        }

        private void GenerateDetailTexture()
        {
            byte[] detailTextureData = new byte[DetailRes * DetailRes * 4];

            new TextureSynth(DetailRes, DetailRes).ApplyWrapNoise(8, 4.0f, 1.0f, h => h < 0f ? -h : h, h => (float)Math.Sqrt(h)).Normalise().WriteComponent(detailTextureData, 0);
            new TextureSynth(DetailRes, DetailRes).ApplyWrapNoise(3, 3.3f, 0.2f, h => h, h => h).Normalise().WriteComponent(detailTextureData, 1);
            new TextureSynth(DetailRes, DetailRes).ApplyWrapNoise(4, 64.0f, 0.1f, h => h < 0f ? -h : h, h => h).Normalise().WriteComponent(detailTextureData, 2);
            new TextureSynth(DetailRes, DetailRes).ApplyWrapNoise(4, 4.0f, 1.0f, h => h < 0f ? -h : h, h => (float)Math.Sqrt(h)).Normalise().WriteComponent(detailTextureData, 3);

            this.TerrainDetailTexture.Upload(detailTextureData);

            this.TerrainDetailTexture.Bind();
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        void TerrainGlobal_Unloading(object sender, EventArgs e)
        {
            this.HeightTexture.Unload();
            this.ShadeTexture.Unload();
            this.IndirectIlluminationTexture.Unload();
            this.TerrainDetailTexture.Unload();
        }


        public void SetDataFromTerrain(TerrainStorage.Terrain terrain)
        {
            if (terrain.Width != this.Width || terrain.Height != this.Height)
            {
                throw new InvalidOperationException("Terrain sizes do not match");
            }

            UploadHeightTextureFromTerrain(terrain);

        }

        public void SetDataFromTerrainGenerationRaw(float[] data)
        {
            if (data.Length < this.Width * this.Height * 4)
            {
                throw new InvalidOperationException("TerrainGlobal.SetDataFromTerrainGenerationRaw: supplied data is too small");
            }
            UploadHeightTextureFromTerrain(data);
        }

        private void UploadHeightTextureFromTerrain(TerrainStorage.Terrain terrain)
        {
            float[] height = new float[this.Width * this.Height];
            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                height[i] = terrain.Map[i].Height;
            });

            UploadHeightTexture(height);
        }

        private void UploadHeightTextureFromTerrain(float[] data)
        {
            float[] height = new float[this.Width * this.Height];
            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                height[i] = data[i * 4] + data[i * 4 + 1] + data[i * 4 + 2];
            });

            UploadHeightTexture(height);
        }


        private void UploadHeightTexture(float[] height)
        {
            this.MinHeight = height.Min();
            this.MaxHeight = height.Max();

            this.HeightTexture.Upload(height);
        }


    }
}
