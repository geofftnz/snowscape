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
    public class TerrainTileLoader : GameComponentBase
    {
        private GBufferShaderStep gb = new GBufferShaderStep("terraintileloader");

        public Texture HeightTexture { get; set; }

        public TerrainTileLoader()
            : base()
        {
            this.Loading += TerrainTileLoader_Loading;
        }

        public TerrainTileLoader(Texture heightTexture)
            : this()
        {
            this.HeightTexture = heightTexture;
        }

        void TerrainTileLoader_Loading(object sender, EventArgs e)
        {
            gb.SetOutputTexture(0, "out_Height", this.HeightTexture);
            gb.Init(@"BasicQuad.vert", @"TerrainTileLoader.frag");
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
