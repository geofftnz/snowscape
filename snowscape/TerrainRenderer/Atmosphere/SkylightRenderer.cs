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
    public class SkylightRenderer : GameComponentBase, IReloadable, IListTextures
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
            gb.Init(@"Skylight.glsl|vs", @"Skylight.glsl|fs");
        }

        public void Reload()
        {
            if (gb != null)
            {
                gb.ReloadShader();
            }
        }


        public void Render(SkyRenderParams p)
        {
            gb.Render(
                () => { },
                (sp) =>
                {
                    sp.SetUniform("eye", p.eye);
                    sp.SetUniform("sunVector", p.sunVector);
                    sp.SetUniform("groundLevel", p.groundLevel);
                    sp.SetUniform("rayleighBrightness", p.rayleighBrightness);
                    sp.SetUniform("rayleighPhase", p.rayleighPhase);
                    sp.SetUniform("mieBrightness", 0.0f);
                    sp.SetUniform("miePhase", 0.8f);
                    //sp.SetUniform("mieBrightness", p.mieBrightness);
                    //sp.SetUniform("miePhase", p.miePhase);
                    sp.SetUniform("scatterAbsorb", p.scatterAbsorb);
                    sp.SetUniform("Kr", p.Kr);
                    sp.SetUniform("sunLight", p.sunLight);
                    sp.SetUniform("skyPrecalcBoundary", p.skyPrecalcBoundary);
                }
                );
        }

        public IEnumerable<Texture> Textures()
        {
            yield return this.SkylightTexture;
        }
    }
}
