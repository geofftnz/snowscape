using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Utils;

namespace Snowscape.TerrainRenderer.Utility
{
    /// <summary>
    /// Copies height and param textures from GPU terrain generation to terrain tile via a FBO.
    /// 
    /// </summary>
    public class TerrainTileTextureCopy
    {
        private GBufferShaderStep gb = new GBufferShaderStep("terrain tex copy");

        public TerrainTileTextureCopy()
        {

        }

        public void Init(Texture heightTexture, Texture paramTexture)
        {
            gb.SetOutputTexture(0, "out_Height", heightTexture);
            gb.SetOutputTexture(1, "out_Param", paramTexture);
            gb.Init(@"../../../Resources/Shaders/GBufferIdentity.vert".Load(), @"../../../Resources/Shaders/TerrainTileTextureCopy.frag".Load());
        }

        public void Render(Texture heightTexture, Texture paramTexture)
        {
            gb.Render(() =>
            {
                heightTexture.Bind(TextureUnit.Texture0);
                paramTexture.Bind(TextureUnit.Texture1);
            },
            (sp) =>
            {
                sp.SetUniform("heightTex", 0);
                sp.SetUniform("paramTex", 1);
            });
        }
    }
}
