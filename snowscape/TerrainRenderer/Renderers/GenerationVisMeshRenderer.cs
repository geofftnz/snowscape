using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL;
using Utils;

namespace Snowscape.TerrainRenderer.Renderers
{
    /// <summary>
    /// Mesh renderer for generation visualisation
    /// 
    /// Simplified renderer with reduced data requirements.
    /// 
    /// Uses height and param textures only.
    /// </summary>
    public class GenerationVisMeshRenderer : ITileRenderer
    {
        private VBO vertexVBO = new VBO("bbvertex");
        private VBO boxcoordVBO = new VBO("bbboxcoord");
        private VBO indexVBO = new VBO("bbindex", BufferTarget.ElementArrayBuffer);
        private ShaderProgram shader = new ShaderProgram("vistilemesh");

        public int Width { get; private set; }
        public int Height { get; private set; }

        public GenerationVisMeshRenderer(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

        public void Load()
        {
            SetupMesh();
            InitShader();
        }

        public void Render(TerrainTile tile, Matrix4 projection, Matrix4 view, Vector3 eyePos)
        {
            var boxparam = tile.GetBoxParam();

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);  // we only want to render back-faces

            tile.HeightTexture.Bind(TextureUnit.Texture0);
            tile.ParamTexture.Bind(TextureUnit.Texture1);

            this.shader
                .UseProgram()
                .SetUniform("projection_matrix", projection)
                .SetUniform("model_matrix", tile.ModelMatrix)
                .SetUniform("view_matrix",view)
                .SetUniform("heightTex", 0)
                .SetUniform("paramTex", 1)
                .SetUniform("eyePos", eyePos)
                .SetUniform("boxparam", boxparam);
            this.vertexVBO.Bind(this.shader.VariableLocation("vertex"));
            this.boxcoordVBO.Bind(this.shader.VariableLocation("in_boxcoord"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);

        }

        public void Unload()
        {
            throw new NotImplementedException();
        }

        private void SetupMesh()
        {
            Vector3[] vertex = new Vector3[this.Width * this.Height];
            Vector3[] boxcoord = new Vector3[this.Width * this.Height];

            // this mesh will leave a gap that must be filled with a strip using data from adjacent tiles.
            float xscale = 1.0f / (float)this.Width;
            float zscale = 1.0f / (float)this.Height;

            
            ParallelHelper.For2D(this.Width, this.Height, (x, z, i) =>
            {
                vertex[i].X = (float)x * xscale;
                vertex[i].Y = 0f;
                vertex[i].Z = (float)z * zscale;

                boxcoord[i].X = (float)x * xscale;
                boxcoord[i].Y = 0f;
                boxcoord[i].Z = (float)z * zscale;
            });

            // vertex VBO
            this.vertexVBO.SetData(vertex);
            // boxcoord VBO
            this.boxcoordVBO.SetData(boxcoord);

            // cubeindex VBO
            uint[] meshindex = new uint[(this.Width - 1) * (this.Height - 1) * 6];

            for (int y = 0; y < this.Height - 1; y++)
            {
                for (int x = 0; x < this.Width - 1; x++)
                {
                    int i = (x + y * (this.Width - 1)) * 6;

                    if (y % 2 == 0)
                    {
                        meshindex[i + 0] = (uint)(x + y * this.Width);  // 0
                        meshindex[i + 1] = (uint)(x + 1 + y * this.Width);  // 1
                        meshindex[i + 2] = (uint)(x + (y + 1) * this.Width); // 2
                        meshindex[i + 3] = (uint)(x + 1 + y * this.Width); // 1
                        meshindex[i + 4] = (uint)(x + 1 + (y + 1) * this.Width); // 3
                        meshindex[i + 5] = (uint)(x + (y + 1) * this.Width); // 2
                    }
                    else
                    {
                        meshindex[i + 0] = (uint)(x + y * this.Width);  // 0
                        meshindex[i + 1] = (uint)(x + 1 + y * this.Width);  // 1
                        meshindex[i + 2] = (uint)(x + 1 + (y + 1) * this.Width); // 3 
                        meshindex[i + 3] = (uint)(x + y * this.Width);  // 0
                        meshindex[i + 4] = (uint)(x + 1 + (y + 1) * this.Width); // 3 
                        meshindex[i + 5] = (uint)(x + (y + 1) * this.Width); // 2
                    }
                }
            }

            indexVBO.SetData(meshindex);

        }

        private void InitShader()
        {
            // setup shader
            this.shader.Init(
                @"../../../Resources/Shaders/GenVisMesh.vert".Load(),
                @"../../../Resources/Shaders/GenVisMesh.frag".Load(),
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "in_boxcoord") 
                },
                new string[]
                {
                    "out_Param",
                    "out_Normal"
                });
        }
    }
}
