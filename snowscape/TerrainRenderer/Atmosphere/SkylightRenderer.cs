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
        private GBufferSimpleStep blur1;
        private GBufferSimpleStep blur2;

        public Texture SkylightTexture { get; private set; }
        public Texture SkylightTexture2 { get; private set; }
        public int SkyRes { get; set; }


        public SkylightRenderer(int resolution = 256)
            : base()
        {
            this.SkyRes = resolution;


            this.Loading += SkylightRenderer_Loading;
            this.Unloading += SkylightRenderer_Unloading;
        }

        void SkylightRenderer_Unloading(object sender, EventArgs e)
        {
            this.SkylightTexture.Unload();
            this.SkylightTexture2.Unload();
        }

        void SkylightRenderer_Loading(object sender, EventArgs e)
        {
            this.SkylightTexture = new Texture("Skylight1", SkyRes, SkyRes, TextureTarget.Texture2D, PixelInternalFormat.Rgb16f, PixelFormat.Rgb, PixelType.HalfFloat);
            this.SkylightTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge));
            this.SkylightTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge));
            this.SkylightTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
            this.SkylightTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear));
            this.SkylightTexture.UploadEmpty();

            this.SkylightTexture2 = new Texture("Skylight2", SkyRes, SkyRes, TextureTarget.Texture2D, PixelInternalFormat.Rgb16f, PixelFormat.Rgb, PixelType.HalfFloat);
            this.SkylightTexture2.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge));
            this.SkylightTexture2.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge));
            this.SkylightTexture2.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
            this.SkylightTexture2.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear));
            this.SkylightTexture2.UploadEmpty();

            gb = new GBufferShaderStep("skylight");
            blur1 = new GBufferSimpleStep("skylight-blur1", @"Skylight.glsl|vs", @"Skylight.glsl|blur1", "skyTex", "out_Sky", this.SkylightTexture2);
            blur2 = new GBufferSimpleStep("skylight-blur2", @"Skylight.glsl|vs", @"Skylight.glsl|blur2", "skyTex", "out_Sky", this.SkylightTexture);

            gb.SetOutputTexture(0, "out_Sky", this.SkylightTexture);
            gb.Init(@"Skylight.glsl|vs", @"Skylight.glsl|fs");

            blur1.Init();
            blur2.Init();
        }

        public void Reload()
        {
            gb.Maybe(a => a.ReloadShader());
            blur1.Maybe(a => a.Reload());
            blur2.Maybe(a => a.Reload());
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
                    
                    //sp.SetUniform("mieBrightness", 0.0f);
                    //sp.SetUniform("miePhase", 0.8f);
                    sp.SetUniform("mieBrightness", p.mieBrightness);
                    sp.SetUniform("miePhase", p.miePhase);

                    sp.SetUniform("scatterAbsorb", p.scatterAbsorb);
                    sp.SetUniform("Kr", p.Kr);
                    sp.SetUniform("sunLight", p.sunLight);
                    sp.SetUniform("skyPrecalcBoundary", p.skyPrecalcBoundary);
                }
                );

            blur1.Render(this.SkylightTexture);
            blur2.Render(this.SkylightTexture2);

        }

        public IEnumerable<Texture> Textures()
        {
            yield return this.SkylightTexture;
            yield return this.SkylightTexture2;
        }
    }
}
