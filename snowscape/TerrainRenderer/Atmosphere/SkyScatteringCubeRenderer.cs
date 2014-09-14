using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utils;
using OpenTKExtensions.Framework;

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
    public class SkyScatteringCubeRenderer : GameComponentBase
    {
        // Needs:
        private GBufferRedirectableShaderStep gb;

        public Texture SkyCubeTexture { get; private set; }
        public int SkyRes { get; set; }


        public SkyScatteringCubeRenderer(int resolution)
            : base()
        {
            this.SkyRes = resolution;

            gb = new GBufferRedirectableShaderStep("sky_scatter_cube", resolution, resolution);

            this.SkyCubeTexture = new Texture(SkyRes, SkyRes, TextureTarget.TextureCubeMap, PixelInternalFormat.Rgb16f, PixelFormat.Rgb, PixelType.HalfFloat);
            this.SkyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge));
            this.SkyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge));
            this.SkyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge));
            this.SkyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
            this.SkyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapNearest));


            this.Loading += SkyScatteringCubeRenderer_Loading;
        }

        void SkyScatteringCubeRenderer_Loading(object sender, EventArgs e)
        {
            SetupCubeMap();

            // TODO: need to find and fix the FBO IncompleteMissingAttachment error
            gb.SetOutputTexture(0, "out_Sky", this.SkyCubeTexture, TextureTarget.TextureCubeMapPositiveX);
            gb.Init(@"SkyScatterCube.vert", @"SkyScatterCube.frag");
        }


        private void SetupCubeMap()
        {
            GL.Enable(EnableCap.TextureCubeMap);
            GL.Enable(EnableCap.TextureCubeMapSeamless);

            this.SkyCubeTexture.UploadEmpty(TextureTarget.TextureCubeMapPositiveX);
            this.SkyCubeTexture.UploadEmpty(TextureTarget.TextureCubeMapPositiveY);
            this.SkyCubeTexture.UploadEmpty(TextureTarget.TextureCubeMapPositiveZ);
            this.SkyCubeTexture.UploadEmpty(TextureTarget.TextureCubeMapNegativeX);
            this.SkyCubeTexture.UploadEmpty(TextureTarget.TextureCubeMapNegativeY);
            this.SkyCubeTexture.UploadEmpty(TextureTarget.TextureCubeMapNegativeZ);
        }


        public void Render(Vector3 eye, Vector3 sunVector, float groundLevel, float raleighBrightness, float mieBrightness, float scatterAbsorb, Vector3 Kr, Vector3 sunLight)
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
                sp.SetUniform("sunLight", sunLight);
            };

            RenderFace(SkyCubeTexture, TextureTarget.TextureCubeMapPositiveX, Vector3.UnitX, -Vector3.UnitZ, -Vector3.UnitY, uniforms);
            RenderFace(SkyCubeTexture, TextureTarget.TextureCubeMapPositiveY, Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ, uniforms);
            RenderFace(SkyCubeTexture, TextureTarget.TextureCubeMapPositiveZ, Vector3.UnitZ, Vector3.UnitX, -Vector3.UnitY, uniforms);
            RenderFace(SkyCubeTexture, TextureTarget.TextureCubeMapNegativeX, -Vector3.UnitX, Vector3.UnitZ, -Vector3.UnitY, uniforms);
            RenderFace(SkyCubeTexture, TextureTarget.TextureCubeMapNegativeY, -Vector3.UnitY, Vector3.UnitX, -Vector3.UnitZ, uniforms);
            RenderFace(SkyCubeTexture, TextureTarget.TextureCubeMapNegativeZ, -Vector3.UnitZ, -Vector3.UnitX, -Vector3.UnitY, uniforms);

            GL.Enable(EnableCap.TextureCubeMap);
            SkyCubeTexture.Bind();
            GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);

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
