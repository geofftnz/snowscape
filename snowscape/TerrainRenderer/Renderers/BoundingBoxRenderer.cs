using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Utils;

namespace Snowscape.TerrainRenderer.Renderers
{

    /// <summary>
    /// 
    /// </summary>
    public class BoundingBoxRenderer : ITileRenderer
    {
        private VBO vertexVBO = new VBO("bbvertex");
        private VBO boxcoordVBO = new VBO("bbboxcoord");
        private VBO indexVBO = new VBO("bbindex", BufferTarget.ElementArrayBuffer);
        private ShaderProgram boundingBoxProgram = new ShaderProgram("bb");

        public BoundingBoxRenderer()
        {

        }

        public void Load()
        {
            SetupBoundingBox();
            InitShader();
        }

        public void Render(TerrainTile tile, Matrix4 projection, Matrix4 view, Vector3 eyePos)
        {
            var boxparam = tile.GetBoxParam();

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);  // we only want to render back-faces

            tile.HeightTexture.Bind(TextureUnit.Texture0);
            tile.NormalTexture.Bind(TextureUnit.Texture1);
            tile.ShadeTexture.Bind(TextureUnit.Texture2);

            this.boundingBoxProgram
                .UseProgram()
                .SetUniform("projection_matrix", projection)
                .SetUniform("modelview_matrix", tile.ModelMatrix * view)
                .SetUniform("heightTex", 0)
                .SetUniform("normalTex", 1)
                .SetUniform("shadeTex", 2)
                .SetUniform("eyePos", eyePos)
                .SetUniform("tileOffset", new Vector3(0f, 0f, 0f))
                .SetUniform("boxparam", boxparam);
            this.vertexVBO.Bind(this.boundingBoxProgram.VariableLocation("vertex"));
            this.boxcoordVBO.Bind(this.boundingBoxProgram.VariableLocation("in_boxcoord"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);

        }

        public void Unload()
        {
        }



        private void SetupBoundingBox()
        {
            float minx, maxx, minz, maxz;
            Vector3[] vertex = new Vector3[8];
            Vector3[] boxcoord = new Vector3[8];

            minx = minz = 0.0f;
            maxx = 1.0f; // width of tile
            maxz = 1.0f; // height of tile

             float minHeight = 0.0f;
             float maxHeight = 1.0f;

            for (int i = 0; i < 8; i++)
            {
                vertex[i].X = (i & 0x02) == 0 ? ((i & 0x01) == 0 ? minx : maxx) : ((i & 0x01) == 0 ? maxx : minx);
                vertex[i].Y = ((i & 0x04) == 0 ? minHeight : maxHeight);
                vertex[i].Z = (i & 0x02) == 0 ? minz : maxz;

                boxcoord[i].X = (i & 0x02) == 0 ? ((i & 0x01) == 0 ? minx : maxx) : ((i & 0x01) == 0 ? maxx : minx);
                boxcoord[i].Y = ((i & 0x04) == 0 ? minHeight : maxHeight);
                boxcoord[i].Z = (i & 0x02) == 0 ? minz : maxz;
            }

            // vertex VBO
            this.vertexVBO.SetData(vertex);
            // boxcoord VBO
            this.boxcoordVBO.SetData(boxcoord);

            // cubeindex VBO
            uint[] cubeindex = {
                                  7,3,2,
                                  7,2,6,
                                  6,2,1,
                                  6,1,5,
                                  5,1,0,
                                  5,0,4,
                                  4,3,7,
                                  4,0,3,
                                  3,1,2,
                                  3,0,1,
                                  5,7,6,
                                  5,4,7
                              };

            indexVBO.SetData(cubeindex);

        }

        private void InitShader()
        {
            // setup shader
            this.boundingBoxProgram.Init(
                @"../../../Resources/Shaders/TerrainTile.vert".Load(),
                @"../../../Resources/Shaders/TerrainTile_Debug1.frag".Load(),
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "in_boxcoord") 
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
