using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Utils;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainRenderer.Lighting
{
    /// <summary>
    /// Takes a heightmap texture and writes normals out to a texture
    /// </summary>
    public class HeightmapNormalGenerator : GameComponentBase
    {
        private GBufferShaderStep gb = new GBufferShaderStep("normalgen");

        public Texture NormalTexture { get; private set; }
        public Texture HeightMap { get; set; }
        public Sampler HeightMapSampler { get; set; }

        public HeightmapNormalGenerator()
        {
        }

        public HeightmapNormalGenerator(Texture normalTexture)
            : this()
        {
            this.NormalTexture = normalTexture;
        }

        public HeightmapNormalGenerator(Texture normalTexture, Texture heightMap)
            : this(normalTexture)
        {
            this.HeightMap = heightMap;
        }

        public override void Load()
        {
            this.LoadWrapper(() =>
            {
                base.Load();

                gb.SetOutputTexture(0, "out_Normal", this.NormalTexture);
                gb.Init(@"HeightmapNormals.vert", @"HeightmapNormals.frag");
            });
            
        }

        public override void Unload()
        {
            this.UnloadWrapper(() => 
            {
                base.Unload();
            });
        }


        public void Render()
        {
            this.Render(this.HeightMap);
        }

        public void Render(Texture heightmap)
        {
            gb.Render(() =>
            {
                heightmap.Bind(TextureUnit.Texture0);
            },
            (sp) =>
            {
                sp.SetUniform("heightmap", 0);
                sp.SetUniform("texsize", (float)heightmap.Width);
            });
        }
        public void Render(Texture heightmap, Sampler heightmapSampler)
        {
            gb.Render(() =>
            {
                heightmapSampler.Bind(TextureUnit.Texture0);
            },
            (sp) =>
            {
                sp.SetUniform("heightmap", 0);
                sp.SetUniform("texsize", (float)heightmap.Width);
            });
        }
    }
}
