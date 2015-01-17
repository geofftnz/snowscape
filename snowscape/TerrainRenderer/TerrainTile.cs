﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;
using OpenTKExtensions.Framework;
using Utils;

namespace Snowscape.TerrainRenderer
{

    /// <summary>
    /// A Terrain Tile...
    /// 
    /// knows about:
    /// - its bounding box (VBO)
    /// - its heightmap (texture), in a state suitable for raycasting.
    /// - its normalmap (texture)
    /// - its shading data (textures)
    /// 
    /// knows how to:
    /// - generate its bounding box from the supplied (full-scale) heightmap
    /// - generate its textures from a subset of the supplied full-scale textures
    /// </summary>
    public class TerrainTile : GameComponentBase, IListTextures
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public Texture HeightTexture { get; private set; }
        public Texture NormalTexture { get; private set; }
        public Texture ShadeTexture { get; private set; }
        public Texture ParamTexture { get; private set; }
        //public Sampler LinearSampler { get; private set; }

        public float MinHeight { get; private set; }
        public float MaxHeight { get; private set; }

        /// <summary>
        /// bounding box for this tile, used for frustum culling
        /// </summary>
        private Vector4[] boundingBox = new Vector4[8];
        public Vector4[] BoundingBox { get { return boundingBox; } }

        private Matrix4 modelMatrix;
        public Matrix4 ModelMatrix
        {
            get { return modelMatrix; }
            set
            {
                modelMatrix = value;
                this.InverseModelMatrix = Matrix4.Invert(modelMatrix);
            }
        }
        public Matrix4 InverseModelMatrix { get; private set; }


        public TerrainTile(int width, int height)
            : base()
        {
            this.Width = width;
            this.Height = height;
            this.SetHeightRange(0f, 1f);

            this.ModelMatrix = Matrix4.Identity;
            this.InitTextures();


            this.Loading += TerrainTile_Loading;
            this.Unloading += TerrainTile_Unloading;
        }

        private void InitTextures()
        {
            this.HeightTexture =
                new Texture("Height", this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            //this.LinearSampler =
            //    new Sampler()
            //    .SetParameter(new SamplerObjectParameterInt(SamplerParameter.TextureMagFilter, (int)TextureMagFilter.Linear))
            //    .SetParameter(new SamplerObjectParameterInt(SamplerParameter.TextureMinFilter, (int)TextureMinFilter.Linear))
            //    .SetParameter(new SamplerObjectParameterInt(SamplerParameter.TextureWrapS, (int)TextureWrapMode.Repeat))
            //    .SetParameter(new SamplerObjectParameterInt(SamplerParameter.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.NormalTexture = new Texture("Normal", this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.ShadeTexture = new Texture("Shade", this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.ParamTexture = new Texture("Param", this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));
        }

        public IEnumerable<Texture> Textures()
        {
            yield return this.HeightTexture;
            yield return this.NormalTexture;
            yield return this.ShadeTexture;
            yield return this.ParamTexture;
        }


        void TerrainTile_Loading(object sender, EventArgs e)
        {
            // setup textures
            this.HeightTexture.UploadEmpty();
            this.NormalTexture.UploadEmpty();
            this.ShadeTexture.UploadEmpty();
            this.ParamTexture.UploadEmpty();
        }

        void TerrainTile_Unloading(object sender, EventArgs e)
        {
            this.HeightTexture.Unload();
            this.NormalTexture.Unload();
            this.ShadeTexture.Unload();
            this.ParamTexture.Unload();
        }


        public void SetDataFromTerrain(TerrainStorage.Terrain terrain, int offsetX, int offsetY)
        {

            SetHeightRange(terrain.Map.Select(c => c.Height).Min(), terrain.Map.Select(c => c.Height).Max());

            // height from cells
            UploadHeightTextureFromTerrain(terrain, offsetX, offsetY);

            // calculate normals on the fly - for visualisation of generation, this will be done in the vertex shader.
            CalculateAndUploadNormalsFromTerrain(terrain, offsetX, offsetY);

            // shade texture - blank for generation vis, AO/shadowmap/scattermap otherwise
            byte[] shade = new byte[this.Width * this.Height * 4];
            this.ShadeTexture.Upload(shade);

            // param texture - cell components
            UploadVisParamTexture(terrain, offsetX, offsetY);
        }

        public void SetDataFromTerrainGeneration(TerrainStorage.Terrain terrain, int offsetX, int offsetY)
        {
            SetHeightRange(terrain.Map.Select(c => c.Height).Min(), terrain.Map.Select(c => c.Height).Max());

            // height from cells
            UploadHeightTextureFromTerrain(terrain, offsetX, offsetY);

            // param texture - cell components
            UploadVisParamTexture(terrain, offsetX, offsetY);
        }

        /// <summary>
        /// Only valid where generated data is the same size as the terrain tile
        /// </summary>
        /// <param name="data"></param>
        public void SetDataFromTerrainGenerationRaw(float[] data)
        {
            // height from cells
            UploadHeightTextureFromTerrain(data);

            // param texture - cell components
            UploadVisParamTexture(data);
        }

        public void SetHeightRange(float minheight, float maxheight)
        {
            this.MinHeight = minheight;
            this.MaxHeight = maxheight;
            this.InitBoundingBox();
        }

        private void InitBoundingBox()
        {
            int i = 0;
            float x = (float)this.Width;
            float z = (float)this.Height;

            // bottom
            // 0 1
            // 2 3  
            //
            // top 
            // 4 5
            // 6 7

            boundingBox[i++] = new Vector4(0f, MinHeight, 0f, 1f);
            boundingBox[i++] = new Vector4(x, MinHeight, 0f, 1f);
            boundingBox[i++] = new Vector4(0f, MinHeight, z, 1f);
            boundingBox[i++] = new Vector4(x, MinHeight, z, 1f);

            boundingBox[i++] = new Vector4(0f, MaxHeight, 0f, 1f);
            boundingBox[i++] = new Vector4(x, MaxHeight, 0f, 1f);
            boundingBox[i++] = new Vector4(0f, MaxHeight, z, 1f);
            boundingBox[i++] = new Vector4(x, MaxHeight, z, 1f);
        }


        private void UploadVisParamTexture(TerrainStorage.Terrain terrain, int offsetX, int offsetY)
        {
            byte[] param = new byte[this.Width * this.Height * 4];

            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                var ii = i * 4;
                int ti = terrain.C(x + offsetX, y + offsetY);
                param[ii + 0] = (byte)(terrain.Map[ti].Loose * 8.0f).Clamp(0f, 255f);
                param[ii + 1] = (byte)(terrain.Map[ti].MovingWater * 8192.0f).Clamp(0f, 255f);
                param[ii + 2] = (byte)(terrain.Map[ti].Carrying * 32.0f).Clamp(0f, 255f);
                param[ii + 3] = (byte)(terrain.Map[ti].Erosion * 0.25f).Clamp(0f, 255f);
            });

            this.ParamTexture.Upload(param);
        }

        private void UploadVisParamTexture(float[] data)
        {
            if (data.Length < this.Width * this.Height * 4)
            {
                throw new InvalidOperationException("TerrainTile.UploadHeightTextureFromTerrain: supplied data is too small");
            }

            byte[] param = new byte[this.Width * this.Height * 4];

            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                var ii = i * 4;
                param[ii + 0] = (byte)(data[ii + 1] * 8.0f).Clamp(0f, 255f);   // soft
                param[ii + 1] = (byte)(data[ii + 2] * 64.0f).Clamp(0f, 255f);  // water
                param[ii + 2] = (byte)(data[ii + 3] * 8192.0f).Clamp(0f, 255f); // suspended
                param[ii + 3] = (byte)(0);
            });

            this.ParamTexture.Upload(param);
        }


        private void CalculateAndUploadNormalsFromTerrain(TerrainStorage.Terrain terrain, int offsetX, int offsetY)
        {
            byte[] normals = new byte[this.Width * this.Height * 4];

            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                var n = terrain.GetNormalFromWHeight(x + offsetX, y + offsetY);
                var ii = i * 4;
                normals[ii + 0] = n.X.UnitToByte();
                normals[ii + 1] = n.Y.UnitToByte();
                normals[ii + 2] = n.Z.UnitToByte();
                normals[ii + 3] = 0;
            });

            this.NormalTexture.Upload(normals);
        }

        private void UploadHeightTextureFromTerrain(TerrainStorage.Terrain terrain, int offsetX, int offsetY)
        {
            float[] height = new float[this.Width * this.Height];
            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                height[i] = terrain.Map[terrain.C(x + offsetX, y + offsetY)].Height;
            });

            UploadHeightTexture(height);
        }

        /// <summary>
        /// only valid where the terrain size is equal to the tile size
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        private void UploadHeightTextureFromTerrain(float[] data)
        {
            if (data.Length < this.Width * this.Height * 4)
            {
                throw new InvalidOperationException("TerrainTile.UploadHeightTextureFromTerrain: supplied data is too small");
            }

            float[] height = new float[this.Width * this.Height];
            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                height[i] = data[i * 4] + data[i * 4 + 1] + data[i * 4 + 2];
            });

            UploadHeightTexture(height);
        }


        public void SetupTestData()
        {
            float[] height = new float[this.Width * this.Height];

            var r = new Random();
            float rx = (float)r.NextDouble();
            float ry = (float)r.NextDouble();

            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                height[i] = Utils.SimplexNoise.wrapfbm((float)x, (float)y, (float)this.Width, (float)this.Height, rx, ry, 10, 0.3f / 256.0f, 80f, h => Math.Abs(h), h => h + h * h);
            });

            // set base level to 0
            float min = height.Min();
            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                height[i] -= min;
            });

            UploadHeightTexture(height);

            // calculate normals
            byte[] normals = new byte[this.Width * this.Height * 4];

            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                var n = GetNormal(height, x, y);
                var ii = i * 4;
                normals[ii + 0] = n.X.UnitToByte();
                normals[ii + 1] = n.Y.UnitToByte();
                normals[ii + 2] = n.Z.UnitToByte();
                normals[ii + 3] = 0;
            });

            this.NormalTexture.Upload(normals);

            // shade texture - write in some noise
            byte[] param = new byte[this.Width * this.Height * 4];

            rx = (float)r.NextDouble();
            ry = (float)r.NextDouble();

            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                var ii = i * 4;
                float s1 = 0.0f;// Utils.SimplexNoise.wrapfbm((float)x, (float)y, (float)this.Width, (float)this.Height, rx, ry, 3, 5.2f / (float)this.Width, 1f, h => Math.Abs(h), h => h + h * h);
                float s2 = 0.5f;//Utils.SimplexNoise.wrapfbm((float)x, (float)y, (float)this.Width, (float)this.Height, rx + 17.0f, ry + 5.0f, 5, 25.2f / (float)this.Width, 1f, h => Math.Abs(h), h => h + h * h);

                param[ii + 0] = s1.UnitToByte();
                param[ii + 1] = s2.UnitToByte();
                param[ii + 2] = 0;
                param[ii + 3] = 0;
            });

            this.ParamTexture.Upload(param);


        }

        private void UploadHeightTexture(float[] height)
        {
            //this.MinHeight = 0.0f;// height.Min();
            //this.MaxHeight = 1.0f;// height.Max();

            int maxlevel = 0;
            int x = this.Width;

            x >>= 1;
            while (x > 0)
            {
                maxlevel++;
                x >>= 1;
            }


            float[] mipleveldata = new float[height.Length];
            height.CopyTo(mipleveldata, 0);

            if (this.HeightTexture.Init() != -1)
            {
                this.HeightTexture.Bind();
                this.HeightTexture.ApplyParameters();

                for (int level = 0; level <= maxlevel; level++)
                {
                    this.HeightTexture.UploadImage(mipleveldata, level);
                    mipleveldata = mipleveldata.GenerateMaximumMipMapLevel(this.Width >> level, this.Width >> level);
                }
            }
        }

        private Vector3 GetNormal(float[] height, int cx, int cy)
        {
            float h1 = height[C(cx, cy - 1)];
            float h2 = height[C(cx, cy + 1)];
            float h3 = height[C(cx - 1, cy)];
            float h4 = height[C(cx + 1, cy)];
            return Vector3.Normalize(new Vector3(h3 - h4, h1 - h2, 2f));
        }

        private int C(int x, int y)
        {
            while (x < 0) x += this.Width;
            while (x >= this.Width) x -= this.Width;
            while (y < 0) y += this.Height;
            while (y >= this.Height) y -= this.Height;
            return x + y * this.Width;
        }




    }
}



