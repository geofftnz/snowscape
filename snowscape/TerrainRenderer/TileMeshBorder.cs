using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;
using Utils;

namespace Snowscape.TerrainRenderer
{

    /// <summary>
    /// The one-cell border required to fill the seam on 2 edges of a mesh tile.
    /// 
    /// knows about:
    /// - the right/bottom border of the current tile
    /// - the left border of the tile to the right
    /// - the top border of the tile below
    /// - the top-left point of the tile to the bottom-right
    /// </summary>
    public class TileMeshBorder
    {
        public TerrainTile Tile { get; set; }

        private VBO vertexVBO = new VBO("bordervertex");
        private VBO boxcoordVBO = new VBO("borderboxcoord");

        // stuff normally specified via textures
        private VBO heightVBO = new VBO("borderheight");
        private VBO normalVBO = new VBO("bordernormal");
        private VBO shadeVBO = new VBO("bordershade");
        private VBO paramVBO = new VBO("borderparam");

        private VBO indexVBO = new VBO("borderindex", BufferTarget.ElementArrayBuffer);
        private ShaderProgram boundingBoxProgram = new ShaderProgram("tilemesh");


        public TileMeshBorder(TerrainTile tile)
        {
            this.Tile = tile;
        }

        public void Init()
        {
            InitShader();
        }

        public void SetData()
        {

        }


        private void InitShader()
        {
            // setup shader
            this.boundingBoxProgram.Init(
                @"TerrainTileMeshBorder.vert",
                @"TerrainTileMeshBorder.frag",
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "in_boxcoord"),
                    new Variable(2, "in_normal"),
                    new Variable(3, "in_shade"),
                    new Variable(4, "in_param"),
                },
                new string[]
                {
                    "out_Pos",
                    "out_Normal",
                    "out_Shade",
                    "out_Param"
                });
        }

    }
}
