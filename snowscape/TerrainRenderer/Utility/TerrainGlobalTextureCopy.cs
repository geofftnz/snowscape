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
    /// Copies height from GPU terrain generation to terrain tile via a FBO.
    /// 
    /// </summary>
    public class TerrainGlobalTextureCopy
    {
        private GBufferShaderStep gb = new GBufferShaderStep("terrain global tex copy");

        public TerrainGlobalTextureCopy()
        {

        }

        public void Init(Texture heightTexture)
        {
            gb.SetOutputTexture(0, "out_Height", heightTexture);
            gb.Init(@"../../../Resources/Shaders/GBufferIdentity.vert".Load(), @"../../../Resources/Shaders/TerrainGlobalTextureCopy.frag".Load());
        }

        public void Render(Texture terrainTexture)
        {
            gb.Render(() =>
            {
                terrainTexture.Bind(TextureUnit.Texture0);
            },
            (sp) =>
            {
                sp.SetUniform("terrainTex", 0);
            });
        }
    }
}
