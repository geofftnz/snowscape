using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL4;
using OpenTK;
using Utils;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainRenderer.Lighting
{
    public class IndirectIlluminationGenerator : GameComponentBase
    {
        private GBufferShaderStep gb = new GBufferShaderStep("indirectillum");

        public Texture OutputTexture { get; set; }

        public IndirectIlluminationGenerator()
            : base()
        {
            this.Loading += IndirectIlluminationGenerator_Loading;
        }

        public IndirectIlluminationGenerator(Texture outputTexture)
            : this()
        {
            this.OutputTexture = outputTexture;
        }


        void IndirectIlluminationGenerator_Loading(object sender, EventArgs e)
        {
            gb.SetOutputTexture(0, "out_Indirect", this.OutputTexture);
            gb.Init(@"IndirectIllumination.vert", @"IndirectIllumination.frag");
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
