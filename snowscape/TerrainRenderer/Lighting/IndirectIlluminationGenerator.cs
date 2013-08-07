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
    public class IndirectIlluminationGenerator
    {
        private GBufferShaderStep gb = new GBufferShaderStep("indirectillum");

        public IndirectIlluminationGenerator()
        {

        }

        public void Init(Texture indirectIlluminationTexture)
        {
            gb.SetOutputTexture(0, "out_Indirect", indirectIlluminationTexture);
            gb.Init(@"../../../Resources/Shaders/IndirectIllumination.vert".Load(), @"../../../Resources/Shaders/IndirectIllumination.frag".Load());
        }

        public void Render(Texture heightmap, Texture shadowheight, Texture normalmap, Vector3 sunVector)
        {
            gb.Render(() =>
            {
                heightmap.Bind(TextureUnit.Texture0);
                shadowheight.Bind(TextureUnit.Texture1);
                normalmap.Bind(TextureUnit.Texture2);
            },
            (sp) =>
            {
                sp.SetUniform("heightTexture", 0);
                sp.SetUniform("shadowTexture", 1);
                sp.SetUniform("normalTexture", 2);
                sp.SetUniform("texsize", (float)heightmap.Width);
                sp.SetUniform("sunVector", sunVector);
            });
        }


    }
}
