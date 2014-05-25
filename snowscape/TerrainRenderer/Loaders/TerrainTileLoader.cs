using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using Utils;
using OpenTK.Graphics.OpenGL;

namespace Snowscape.TerrainRenderer.Loaders
{
    public class TerrainTileLoader
    {
        private GBufferShaderStep gb = new GBufferShaderStep("terraintileloader");

        public TerrainTileLoader()
        {
                
        }

        public void Init(Texture heightTexture)
        {
            gb.SetOutputTexture(0, "out_Height", heightTexture);
            gb.Init(@"BasicQuad.vert", @"TerrainTileLoader.frag");
        }

        public void Render(Texture terrainTexture, float waterHeightScale = 1.0f)
        {
            gb.Render(() =>
            {
                terrainTexture.Bind(TextureUnit.Texture0);
            },
            (sp) =>
            {
                sp.SetUniform("terraintex", 0);
                sp.SetUniform("waterHeightScale", waterHeightScale);
            });
        }

    }
}
