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
    /// SkylightRenderer - pre-calculates ambient map for sky light
    /// 
    /// Needs: 
    /// - same stuff as sky cube renderer
    /// 
    /// </summary>
    public class SkylightRenderer : GameComponentBase, IReloadable
    {
        // Needs:
        private GBufferShaderStep gb;

        public Texture SkylightTexture { get; private set; }
        public int SkyRes { get; set; }


        public SkylightRenderer(int resolution = 256)
            : base()
        {
            this.SkyRes = resolution;

            this.SkylightTexture = new Texture(SkyRes, SkyRes, TextureTarget.Texture2D, PixelInternalFormat.Rgb16f, PixelFormat.Rgb, PixelType.HalfFloat);
            this.SkylightTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge));
            this.SkylightTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge));
            this.SkylightTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
            this.SkylightTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear));
            this.SkylightTexture.UploadEmpty();

            gb = new GBufferShaderStep("skylight");

            this.Loading += SkylightRenderer_Loading;
        }

        void SkylightRenderer_Loading(object sender, EventArgs e)
        {
            gb.SetOutputTexture(0, "out_Sky", this.SkylightTexture);
            gb.Init(@"SkyScatterCube.vert", @"SkyScatterCube.frag");
        }

        public void Reload()
        {
            if (gb != null)
            {
                gb.ReloadShader();
            }
        }




        public void Render(Vector3 eye, Vector3 sunVector, float groundLevel, float rayleighPhase, float rayleighBrightness, float miePhase, float mieBrightness, float scatterAbsorb, Vector3 Kr, Vector3 sunLight, float skyPrecalcBoundary)
        {

        }

        private void RenderFace(Texture cubeMapTex, TextureTarget target, Vector3 facenormal, Vector3 facexbasis, Vector3 faceybasis, Action<ShaderProgram> uniforms)
        {
        }

    }
}
