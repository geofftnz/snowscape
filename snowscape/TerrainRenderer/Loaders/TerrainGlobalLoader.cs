using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using Utils;
using OpenTK.Graphics.OpenGL4;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainRenderer.Loaders
{
    public class TerrainGlobalLoader : GameComponentBase
    {
        private GBufferShaderStep gb = new GBufferShaderStep("terraingloballoader");
        public Texture HeightTexture { get; set; }

        public TerrainGlobalLoader()
            : base()
        {
            this.Loading += TerrainGlobalLoader_Loading;
        }

        public TerrainGlobalLoader(Texture heightTexture)
            : this()
        {
            this.HeightTexture = heightTexture;
        }

        void TerrainGlobalLoader_Loading(object sender, EventArgs e)
        {
            if (this.HeightTexture == null)
            {
                throw new InvalidOperationException("TerrainGlobalLoader - height texture not set");
            }
            gb.SetOutputTexture(0, "out_Height", this.HeightTexture);
            gb.Init(@"BasicQuad.vert", @"TerrainGlobalLoader.frag");
        }

        public void Render(Texture terrainTexture, float waterHeightScale = 1.0f)
        {
            gb.Render(() =>
            {
                terrainTexture.Bind(TextureUnit.Texture0);
            },
            (sp) =>
            {
                sp.SetUniform("terraintex", 0);
                sp.SetUniform("waterHeightScale", waterHeightScale);
            });
        }

    }
}
