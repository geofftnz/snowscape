using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using Utils;
using OpenTK.Graphics.OpenGL;

namespace Snowscape.TerrainRenderer.Loaders
{
    public class TerrainTileParamLoader
    {
        private GBufferShaderStep gb = new GBufferShaderStep("terraintileparamloader");

        public TerrainTileParamLoader()
        {
                
        }

        public void Init(Texture paramTexture)
        {
            gb.SetOutputTexture(0, "out_Param", paramTexture);
            gb.Init(@"../../../Resources/Shaders/BasicQuad.vert".Load(), @"../../../Resources/Shaders/TerrainTileParamLoader.frag".Load());
        }

        public void Render(Texture terrainTexture)
        {
            gb.Render(() =>
            {
                terrainTexture.Bind(TextureUnit.Texture0);
            },
            (sp) =>
            {
                sp.SetUniform("terraintex", 0);
            });
        }

    }
}
