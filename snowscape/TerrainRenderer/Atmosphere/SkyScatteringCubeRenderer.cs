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
    public class SkyScatteringCubeRenderer : GameComponentBase, IReloadable
    {
        // Needs:
        private GBufferRedirectableShaderStep gb;

        public Texture SkyCubeTexture { get; private set; }
        public int SkyRes { get; set; }


        public SkyScatteringCubeRenderer(int resolution)
            : base()
        {
            this.SkyRes = resolution;

            this.SkyCubeTexture = new Texture(SkyRes, SkyRes, TextureTarget.TextureCubeMap, PixelInternalFormat.Rgb16f, PixelFormat.Rgb, PixelType.HalfFloat);
            this.SkyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge));
            this.SkyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge));
            this.SkyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge));
            this.SkyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
            this.SkyCubeTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapNearest));

            gb = new GBufferRedirectableShaderStep("sky_scatter_cube", resolution, resolution);

            this.Loading += SkyScatteringCubeRenderer_Loading;
        }

        void SkyScatteringCubeRenderer_Loading(object sender, EventArgs e)
        {
            SetupCubeMap();

            var tex = new Texture(SkyRes, SkyRes, TextureTarget.Texture2D, PixelInternalFormat.Rgb16f, PixelFormat.Rgb,PixelType.HalfFloat);
            tex.UploadEmpty();

            // TODO: need to find and fix the FBO IncompleteMissingAttachment error
            gb.SetOutputTexture(0, "out_Sky", tex, TextureTarget.Texture2D);
            gb.Init(@"SkyScatterCube.vert", @"SkyScatterCube.frag");
            gb.ClearOutputTexture(0);

            tex.Unload();
        }

        public void Reload()
        {
            if (gb != null)
            {
                gb.ReloadShader();
            }
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



        //Vector3 eye, Vector3 sunVector, float groundLevel, float rayleighPhase, float rayleighBrightness, float miePhase, float mieBrightness, float scatterAbsorb, Vector3 Kr, Vector3 sunLight, float skyPrecalcBoundary
        public void Render(SkyRenderParams p)
        {
            Action<ShaderProgram> uniforms = (sp) =>
            {
                sp.SetUniform("eye", p.eye);
                sp.SetUniform("sunVector", p.sunVector);
                sp.SetUniform("groundLevel", p.groundLevel);
                sp.SetUniform("rayleighBrightness", p.rayleighBrightness);
                sp.SetUniform("mieBrightness", p.mieBrightness);
                sp.SetUniform("rayleighPhase", p.rayleighPhase);
                sp.SetUniform("miePhase", p.miePhase);
                sp.SetUniform("scatterAbsorb", p.scatterAbsorb);
                sp.SetUniform("Kr", p.Kr);
                sp.SetUniform("sunLight", p.sunLight);
                sp.SetUniform("skyPrecalcBoundary", p.skyPrecalcBoundary);
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
