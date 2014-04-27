using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utils;

namespace Snowscape.TerrainRenderer.Atmosphere
{
    /// <summary>
    /// SkyScatteringRenderer - pre-calculates the sky into a single texture.
    /// 
    /// Needs: 
    /// - sun direction
    /// - ground level
    /// 
    /// </summary>
    public class SkyScatteringRenderer
    {
        // Needs:
        private GBufferShaderStep gb = new GBufferShaderStep("sky_scatter");

        public SkyScatteringRenderer()
        {
        }

        public void Init(Texture outputTexture)
        {
            gb.SetOutputTexture(0, "out_Sky", outputTexture);
            gb.Init(@"SkyScatter.vert", @"SkyScatter.frag");

        }

        public void Render(Vector3 eye, Vector3 sunVector, float groundLevel, float raleighBrightness, float mieBrightness, float scatterAbsorb, Vector3 Kr)
        {
            gb.Render(() =>
            {
            },
            (sp) =>
            {
                sp.SetUniform("eye", eye);
                sp.SetUniform("sunVector", sunVector);
                sp.SetUniform("groundLevel", groundLevel);
                sp.SetUniform("raleighBrightness", raleighBrightness);
                sp.SetUniform("mieBrightness", mieBrightness);
                sp.SetUniform("scatterAbsorb", scatterAbsorb);
                sp.SetUniform("Kr", Kr);
            });

        }
    }
}
