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
    public class SkyScatteringCubeRenderer
    {
        // Needs:
        private GBufferRedirectableShaderStep gb;

        public SkyScatteringCubeRenderer(int resolution)
        {
            gb = new GBufferRedirectableShaderStep("sky_scatter_cube", resolution, resolution);
        }

        public void Init()
        {
            gb.Init(@"../../../Resources/Shaders/SkyScatterCube.vert".Load(), @"../../../Resources/Shaders/SkyScatterCube.frag".Load());
        }

        public void Render(Texture cubeMapTex, Vector3 eye, Vector3 sunVector, float groundLevel, float raleighBrightness, float mieBrightness, float scatterAbsorb, Vector3 Kr)
        {
            Action<ShaderProgram> uniforms = (sp) =>
            {
                sp.SetUniform("eye", eye);
                sp.SetUniform("sunVector", sunVector);
                sp.SetUniform("groundLevel", groundLevel);
                sp.SetUniform("raleighBrightness", raleighBrightness);
                sp.SetUniform("mieBrightness", mieBrightness);
                sp.SetUniform("scatterAbsorb", scatterAbsorb);
                sp.SetUniform("Kr", Kr);
            };

            RenderFace(cubeMapTex, TextureTarget.TextureCubeMapPositiveX, Vector3.UnitX, -Vector3.UnitZ, -Vector3.UnitY, uniforms);
            RenderFace(cubeMapTex, TextureTarget.TextureCubeMapPositiveY, Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ, uniforms);
            RenderFace(cubeMapTex, TextureTarget.TextureCubeMapPositiveZ, Vector3.UnitZ, Vector3.UnitX, -Vector3.UnitY, uniforms);
            RenderFace(cubeMapTex, TextureTarget.TextureCubeMapNegativeX, -Vector3.UnitX, Vector3.UnitZ, -Vector3.UnitY, uniforms);
            RenderFace(cubeMapTex, TextureTarget.TextureCubeMapNegativeY, -Vector3.UnitY, Vector3.UnitX, -Vector3.UnitZ, uniforms);
            RenderFace(cubeMapTex, TextureTarget.TextureCubeMapNegativeZ, -Vector3.UnitZ, -Vector3.UnitX, -Vector3.UnitY, uniforms);

        }

        private void RenderFace(Texture cubeMapTex, TextureTarget target, Vector3 facenormal, Vector3 facexbasis, Vector3 faceybasis, Action<ShaderProgram> uniforms)
        {
            gb.Render(() =>
            {
            },
            (sp) =>
            {
                uniforms(sp);
                sp.SetUniform("facenormal", facenormal);
                sp.SetUniform("facexbasis", facexbasis);
                sp.SetUniform("faceybasis", faceybasis);
            },
            new GBuffer.TextureSlot(0, cubeMapTex, target)
            );
        }
    }
}
