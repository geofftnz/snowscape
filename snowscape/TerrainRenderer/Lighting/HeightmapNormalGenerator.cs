using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Utils;

namespace Snowscape.TerrainRenderer.Lighting
{
    /// <summary>
    /// Takes a heightmap texture and writes normals out to a texture
    /// </summary>
    public class HeightmapNormalGenerator
    {
        private GBufferShaderStep gb = new GBufferShaderStep("normalgen");

        public HeightmapNormalGenerator()
        {

        }

        public void Init(Texture normalTexture)
        {
            gb.SetOutputTexture(0, "out_Normal", normalTexture);
            gb.Init(@"../../../Resources/Shaders/HeightmapNormals.vert".Load(), @"../../../Resources/Shaders/HeightmapNormals.frag".Load());
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
