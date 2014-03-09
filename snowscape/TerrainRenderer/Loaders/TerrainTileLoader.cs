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

        public void Init(Texture heightTexture, Texture paramTexture)
        {
            gb.SetOutputTexture(0, "out_Height", heightTexture);
            gb.SetOutputTexture(1, "out_Param", paramTexture);
            gb.Init(@"../../../Resources/Shaders/BasicQuad.vert".Load(), @"../../../Resources/Shaders/TerrainTileLoader.frag".Load());
        }

        public void Render(Texture terrainTexture)
        {
            gb.Render(() =>
            {
                terrainTexture.Bind(TextureUnit.Texture0);
                //paramTexture.Bind(TextureUnit.Texture1);
            },
            (sp) =>
            {
                sp.SetUniform("terraintex", 0);
                //sp.SetUniform("paramtex", 1);
            });
        }

    }
}
