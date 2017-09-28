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
    public class TerrainTileParamLoader : GameComponentBase
    {
        private GBufferShaderStep gb = new GBufferShaderStep("terraintileparamloader");

        public Texture ParamTexture { get; set; }

        public TerrainTileParamLoader()
            : base()
        {
            this.Loading += TerrainTileParamLoader_Loading;
        }

        public TerrainTileParamLoader(Texture paramTexture)
            : this()
        {
            this.ParamTexture = paramTexture;
        }

        void TerrainTileParamLoader_Loading(object sender, EventArgs e)
        {
            gb.SetOutputTexture(0, "out_Param", this.ParamTexture);
            gb.Init(@"BasicQuad.vert", @"TerrainTileParamLoader.frag");
        }


        public void Render(Texture terrainTexture)
        {
            gb.Render(() =>
            {
                terrainTexture.Bind(TextureUnit.Texture0);
            },
            (sp) =>
            {
                sp.SetUniform("terraintex", 0);
            });
        }

    }
}
