using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL;
using Utils;
using Snowscape.TerrainRenderer.Mesh;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainRenderer.Renderers
{
    /// <summary>
    /// Wireframe renderer for a subset of a tile (ie: a patch)
    /// 
    /// This will use a mesh and a portion of a heightmap and associated textures.
    /// This renderer outputs a wireframe used to debug/tune the quadtree.
    /// </summary>
    public class WireframePatchRenderer : GameComponentBase, ITileRenderer, IPatchRenderer
    {
        //private TerrainPatchMesh mesh;

        const int MESHRES = 33;  // power of 2 + 1
        private VBO vertexVBO = new VBO("wfpatch-vertex");
        private VBO boxcoordVBO = new VBO("wfpatch-boxcoord");
        private VBO indexVBO = new VBO("wfpatch-index", BufferTarget.ElementArrayBuffer);


        private ShaderProgram shader = new ShaderProgram("wfpatch");

        public IPatchCache PatchCache { get; set; }

        /// <summary>
        /// Sets the width of the patch. This will fetch (and potentially generate) the correct-sized patch mesh from the current patch cache.
        /// </summary>
        public int Width
        {
            get
            {
                return _width;
            }
            set
            {
                if (value != _width)
                {
                    _width = value;
                }
            }
        }
        private int _width;

        public int Height { get; set; }

        public float Scale { get; set; }
        public Vector2 Offset { get; set; }
        public float DetailScale { get; set; }
        public Texture DetailTexture { get; set; }
        public float DetailTexScale { get; set; }

        public WireframePatchRenderer(int width, int height, IPatchCache patchCache)
            : base()
        {
            if (width != height)
            {
                throw new InvalidOperationException("Patch must be square.");
            }
            this.PatchCache = patchCache;
            this.Width = width;
            this.Height = height;
            this.Scale = 1.0f;
            this.Offset = Vector2.Zero;
            this.DetailScale = 1.0f;
            this.DetailTexScale = 0.1f;

            this.Loading += GenerationVisPatchRenderer_Loading;
        }

        void GenerationVisPatchRenderer_Loading(object sender, EventArgs e)
        {
            InitShader();
            InitMesh();
        }

        private Vector3 GetVertex(int x, int y)
        {
            float scale = 1.0f / (float)(MESHRES - 1);
            return new Vector3(
                (float)x * scale,
                0.0f,
                (float)y * scale
                );
        }
        private Vector3 GetBoxcoord(int x, int y)
        {
            float scale = 1.0f / (float)(MESHRES - 1);
            return new Vector3(
                (float)x * scale,
                0.0f,
                (float)y * scale
                );
        }


        private void InitMesh()
        {
            int vertexcount = MESHRES * 4;  // each border
            int indexcount = (MESHRES - 1) * 2 * 4;

            Vector3[] vertex = new Vector3[vertexcount];
            Vector3[] boxcoord = new Vector3[vertexcount];
            uint[] meshindex = new uint[indexcount];

            int ii = 0;

            // top
            for (int i = 0; i < MESHRES; i++)
            {
                vertex[ii] = GetVertex(i, 0);
                boxcoord[ii] = GetBoxcoord(i, 0);
                ii++;
            }
            // bottom
            for (int i = 0; i < MESHRES; i++)
            {
                vertex[ii] = GetVertex(i, MESHRES - 1);
                boxcoord[ii] = GetBoxcoord(i, MESHRES - 1);
                ii++;
            }
            // left
            for (int i = 0; i < MESHRES; i++)
            {
                vertex[ii] = GetVertex(0, i);
                boxcoord[ii] = GetBoxcoord(0, i);
                ii++;
            }
            // right
            for (int i = 0; i < MESHRES; i++)
            {
                vertex[ii] = GetVertex(0, i);
                boxcoord[ii] = GetBoxcoord(0, i);
                ii++;
            }

            /*
            for (int i = 0; i < indexcount ; i+=2)
            {
                meshindex[i] = (uint)i;
                meshindex[i + 1] = (uint)i + 1u;
            }*/

            ii = 0;

            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < MESHRES-1; i++)
                {
                    meshindex[ii++] = (uint)((side * MESHRES) + i);
                    meshindex[ii++] = (uint)((side * MESHRES) + i + 1);
                }
            }


            // vertex VBO
            this.vertexVBO.SetData(vertex);
            // boxcoord VBO
            this.boxcoordVBO.SetData(boxcoord);

            indexVBO.SetData(meshindex);

        }


        private void InitShader()
        {
            // setup shader
            this.shader.Init(
                @"WireframePatch.glsl|VS",
                @"WireframePatch.glsl|FS",
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "in_boxcoord") 
                },
                new string[]
                {
                    "out_Param",
                    "out_Normal",
                    "out_NormalLargeScale"
                });
        }

        public void Render(TerrainTile tile, Matrix4 projection, Matrix4 view, Vector3 eyePos)
        {
            var boxparam = tile.GetBoxParam();

            GL.Disable(EnableCap.CullFace);
            //GL.CullFace(CullFaceMode.Back);  // we only want to render front-faces

            tile.HeightTexture.Bind(TextureUnit.Texture0);
            tile.ParamTexture.Bind(TextureUnit.Texture1);
            tile.NormalTexture.Bind(TextureUnit.Texture2);

            if (this.DetailTexture != null)
            {
                this.DetailTexture.Bind(TextureUnit.Texture3);
            }

            this.shader
                .UseProgram()
                .SetUniform("projection_matrix", projection)
                .SetUniform("model_matrix", tile.ModelMatrix)
                .SetUniform("view_matrix", view)
                .SetUniform("heightTex", 0)
                .SetUniform("paramTex", 1)
                .SetUniform("normalTex", 2)
                .SetUniform("detailTex", 3)
                .SetUniform("eyePos", eyePos)
                .SetUniform("boxparam", boxparam)
                .SetUniform("patchSize", this.Width)
                .SetUniform("scale", this.Scale)
                .SetUniform("offset", this.Offset)
                .SetUniform("detailTexScale", this.DetailTexScale);
            //this.mesh.Bind(this.shader.VariableLocation("vertex"), this.shader.VariableLocation("in_boxcoord"));
            //this.mesh.Render();
            this.vertexVBO.Bind(this.shader.VariableLocation("vertex"));
            this.boxcoordVBO.Bind(this.shader.VariableLocation("in_boxcoord"));
            this.indexVBO.Bind();

            GL.DrawElements(BeginMode.Lines, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);

            //Sampler.Unbind(TextureUnit.Texture0);

        }
    }
}
