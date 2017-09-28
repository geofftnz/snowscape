using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL4;
using Utils;
using Snowscape.TerrainRenderer.Mesh;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainRenderer.Renderers
{
    /// <summary>
    /// Renderer for a subset of a tile (ie: a patch)
    /// 
    /// This will use a mesh and a portion of a heightmap and associated textures.
    /// The mesh is designed to seamlessly tile to adjacent patches, assuming the source textures wrap around.
    /// </summary>
    public class GenerationVisPatchLowRenderer : GameComponentBase, ITileRenderer, IPatchRenderer
    {
        private TerrainPatchMesh mesh;
        private ShaderProgram shader = new ShaderProgram("vistilepatch");

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
                    this.mesh = PatchCache.GetPatchMesh(_width);
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

        public GenerationVisPatchLowRenderer(int width, int height, IPatchCache patchCache)
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

            this.Loading += GenerationVisPatchLowRenderer_Loading;
        }

        void GenerationVisPatchLowRenderer_Loading(object sender, EventArgs e)
        {
            InitShader();
        }

        private void InitShader()
        {
            // setup shader
            this.shader.Init(
                @"GenVisPatchLow.vert",
                @"GenVisPatchLow.frag",
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

        public void Render(TerrainTile tile, TerrainGlobal terrainGlobal, Matrix4 projection, Matrix4 view, Vector3 eyePos)
        {
            var boxparam = tile.GetBoxParam();

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);  // we only want to render front-faces

            tile.HeightTexture.Bind(TextureUnit.Texture0);
            tile.ParamTexture.Bind(TextureUnit.Texture1);
            tile.NormalTexture.Bind(TextureUnit.Texture2);

            this.shader
                .UseProgram()
                .SetUniform("projection_matrix", projection)
                .SetUniform("model_matrix", tile.ModelMatrix)
                .SetUniform("view_matrix", view)
                .SetUniform("heightTex", 0)
                .SetUniform("paramTex", 1)
                .SetUniform("normalTex", 2)
                .SetUniform("eyePos", eyePos)
                .SetUniform("boxparam", boxparam)
                .SetUniform("patchSize", this.Width)
                .SetUniform("scale", this.Scale)
                .SetUniform("offset", this.Offset);
            this.mesh.Bind(this.shader.VariableLocation("vertex"), this.shader.VariableLocation("in_boxcoord"));
            this.mesh.Render();

        }

    }
}
