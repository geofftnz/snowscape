using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;
using OpenTKExtensions.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snowscape.TerrainRenderer.TerrainDetail
{

    /// <summary>
    /// Generates a higher level of detail from the base terrain maps.
    /// 
    /// On render
    /// </summary>
    public class DetailGenerator : GameComponentBase, IListTextures, IReloadable
    {
        // output textures 
        public Texture HeightTexture { get; private set; }
        public Texture NormalTexture { get; private set; }
        public Texture ParamTexture { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        // shader step for detail generation
        private GBufferShaderStep gb = new GBufferShaderStep("detailgen");

        /// <summary>
        /// Current position of center in world coordinates. 
        /// (This will get multiplied up by scale.)
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// last generated position
        /// </summary>
        //private Vector2 LastPosition = new Vector2(-1f, -1f);
        

        /// <summary>
        /// Scale factor of detail generation (likely to be 16)
        /// </summary>
        public float Scale { get; set; }

        public DetailGenerator(int width, int height)
            : base()
        {
            this.Width = width;
            this.Height = height;

            this.Loading += DetailGenerator_Loading;
            this.Unloading += DetailGenerator_Unloading;
        }

        void DetailGenerator_Unloading(object sender, EventArgs e)
        {
            this.HeightTexture.Unload();
            this.NormalTexture.Unload();
            this.ParamTexture.Unload();
        }

        void DetailGenerator_Loading(object sender, EventArgs e)
        {
            this.HeightTexture =
                new Texture("Height", this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.NormalTexture = new Texture("Normal", this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.ParamTexture = new Texture("Param", this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.HeightTexture.UploadEmpty();
            this.NormalTexture.UploadEmpty();
            this.ParamTexture.UploadEmpty();
            
            gb.SetOutputTexture(0, "out_Height", this.HeightTexture);
            gb.SetOutputTexture(1, "out_Normal", this.NormalTexture);
            gb.SetOutputTexture(2, "out_Param", this.ParamTexture);
            gb.Init(@"DetailGenerator.glsl|vert", @"DetailGenerator.glsl|frag");
        }

        public IEnumerable<Texture> Textures()
        {
            yield return HeightTexture;
            yield return NormalTexture;
            yield return ParamTexture;
        }

        public void Render(Texture inputHeightTexture, Texture inputParamTexture)
        {
            gb.Render(() =>
            {
                inputHeightTexture.Bind(TextureUnit.Texture0);
                inputParamTexture.Bind(TextureUnit.Texture1);
            },
            (sp) =>
            {
                sp.SetUniform("heightTex", 0);
                sp.SetUniform("paramTex", 1);
                sp.SetUniform("texsize", (float)inputHeightTexture.Width);
                sp.SetUniform("invtexsize", 1.0f/(float)inputHeightTexture.Width);
                sp.SetUniform("position", Position);
            });

        }



        public void Reload()
        {
            gb.ReloadShader();
        }
    }
}

