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
    /// CloudDepthRenderer - pre-calculates the optical depth of the cloud layer with respect to the light source.
    /// 
    /// Knows about:
    /// - nothing
    /// 
    /// Knows how to:
    /// - calculate the min, max and relative density of the cloud layer.
    /// 
    /// Needs:
    /// - sun direction
    /// - xz scale factor: cloudScale.xz
    /// - cloud layer thickness: cloudScale.y
    ///  (need to convert the 0-1 of texel-space into world space)
    /// 
    /// </summary>
    public class CloudDepthRenderer
    {
        // Needs:
        private GBufferShaderStep gb = new GBufferShaderStep("cloud_depth");


        public CloudDepthRenderer()
        {
        }

        public void Init(Texture outputTexture)
        {
            gb.SetOutputTexture(0, "out_CloudDepth", outputTexture);
            gb.Init(@"CloudDepth.vert", @"CloudDepth.frag");

        }

        public void Render(Texture cloudTexture, Vector3 sunVector, Vector3 cloudScale)
        {
            gb.Render(() =>
            {
                cloudTexture.Bind(TextureUnit.Texture0);
            },
            (sp) =>
            {
                sp.SetUniform("cloudTexture", 0);
                sp.SetUniform("sunDirection", sunVector);
                sp.SetUniform("cloudScale", cloudScale);
            });
        }
    }
}
