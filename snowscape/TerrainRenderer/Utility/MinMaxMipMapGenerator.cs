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
    public class MinMaxMipMapGenerator
    {
        private GBufferRedirectableShaderStep gb = new GBufferRedirectableShaderStep("minmax",1024,1024);

        public MinMaxMipMapGenerator()
        {

        }

        public void Init()
        {
            gb.Init(@"../../../Resources/Shaders/GBufferIdentity.vert".Load(), @"../../../Resources/Shaders/MinMaxMipMap.frag".Load());
        }

        public void Render(Texture heightTexture, int baseLevel, float baseLevelWidth)
        {
            gb.Render(() =>
            {
                heightTexture.Bind(TextureUnit.Texture0);
            },
            (sp) =>
            {
                sp.SetUniform("heightTexture", 0);
                sp.SetUniform("baseLevel", baseLevel);
                sp.SetUniform("baseLevelWidth", baseLevelWidth);
            },

            new GBuffer.TextureSlot(0, heightTexture, baseLevel + 1)

            );
        }
    }
}
