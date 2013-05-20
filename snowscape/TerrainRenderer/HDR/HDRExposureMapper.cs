using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utils;

namespace Snowscape.TerrainRenderer.HDR
{
    public class HDRExposureMapper
    {
        private GBufferShaderStep gb = new GBufferShaderStep("hdr");

        public HDRExposureMapper()
        {

        }

        public void Init(Texture outputTexture)
        {
            gb.SetOutputTexture(0, "out_Colour", outputTexture);
            gb.Init(@"../../../Resources/Shaders/HDRExpose.vert".Load(), @"../../../Resources/Shaders/HDRExpose.frag".Load());
        }

        public void Render(Texture inputTexture, float exposure)
        {
            gb.Render(() =>
            {
                inputTexture.Bind(TextureUnit.Texture0);
            },
            (sp) =>
            {
                sp.SetUniform("exposure", exposure);
                sp.SetUniform("colTex", 0);
            });

        }
    }
}
