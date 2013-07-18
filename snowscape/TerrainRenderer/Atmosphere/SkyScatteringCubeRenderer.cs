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

        public void Render(Texture cubeMapTexDest, Texture cubeMapTexSrc, Vector3 eye, Vector3 sunVector, float groundLevel, float raleighBrightness, float mieBrightness, float scatterAbsorb, Vector3 Kr, Vector3 sunLight, float time)
        {
            Action<ShaderProgram> uniforms = (sp) =>
            {
                sp.SetUniform("prevSky", 0);
                sp.SetUniform("time",time);
                sp.SetUniform("eye", eye);
                sp.SetUniform("sunVector", sunVector);
                sp.SetUniform("groundLevel", groundLevel);
                sp.SetUniform("raleighBrightness", raleighBrightness);
                sp.SetUniform("mieBrightness", mieBrightness);
                sp.SetUniform("scatterAbsorb", scatterAbsorb);
                sp.SetUniform("Kr", Kr);
                sp.SetUniform("sunLight", sunLight);
            };

            RenderFace(cubeMapTexDest, cubeMapTexSrc, TextureTarget.TextureCubeMapPositiveX, Vector3.UnitX, -Vector3.UnitZ, -Vector3.UnitY, uniforms);
            RenderFace(cubeMapTexDest, cubeMapTexSrc, TextureTarget.TextureCubeMapPositiveY, Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ, uniforms);
            RenderFace(cubeMapTexDest, cubeMapTexSrc, TextureTarget.TextureCubeMapPositiveZ, Vector3.UnitZ, Vector3.UnitX, -Vector3.UnitY, uniforms);
            RenderFace(cubeMapTexDest, cubeMapTexSrc, TextureTarget.TextureCubeMapNegativeX, -Vector3.UnitX, Vector3.UnitZ, -Vector3.UnitY, uniforms);
            RenderFace(cubeMapTexDest, cubeMapTexSrc, TextureTarget.TextureCubeMapNegativeY, -Vector3.UnitY, Vector3.UnitX, -Vector3.UnitZ, uniforms);
            RenderFace(cubeMapTexDest, cubeMapTexSrc, TextureTarget.TextureCubeMapNegativeZ, -Vector3.UnitZ, -Vector3.UnitX, -Vector3.UnitY, uniforms);

            GL.Enable(EnableCap.TextureCubeMap);
            cubeMapTexDest.Bind();
            GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);

        }

        private void RenderFace(Texture cubeMapTexDest, Texture cubeMapTexSrc, TextureTarget target, Vector3 facenormal, Vector3 facexbasis, Vector3 faceybasis, Action<ShaderProgram> uniforms)
        {
            gb.Render(() =>
            {
                cubeMapTexSrc.Bind(TextureUnit.Texture0);
            },
            (sp) =>
            {
                uniforms(sp);
                sp.SetUniform("facenormal", facenormal);
                sp.SetUniform("facexbasis", facexbasis);
                sp.SetUniform("faceybasis", faceybasis);
            },
            new GBuffer.TextureSlot(0, cubeMapTexDest, target)
            );
        }
    }
}
