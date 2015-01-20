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
    /// Renderer for a subset of a tile (ie: a patch)
    /// 
    /// This will use a mesh and a portion of a heightmap and associated textures.
    /// The mesh is designed to seamlessly tile to adjacent patches, assuming the source textures wrap around.
    /// </summary>
    public class PatchHighRenderer : GameComponentBase, ITileRenderer, IPatchRenderer, IReloadable
    {
        private TerrainPatchMesh mesh;
        private ShaderProgram shader = new ShaderProgram("patchhigh");

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

        public PatchHighRenderer(int width, int height, IPatchCache patchCache)
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

            this.Loading += PatchHighRenderer_Loading;
        }

        void PatchHighRenderer_Loading(object sender, EventArgs e)
        {
            Reload();
        }

        private ShaderProgram LoadShader()
        {
            var program = new ShaderProgram(this.GetType().Name);
            // setup shader
            program.Init(
                @"PatchRender.glsl|HighVertex",
                @"PatchRender.glsl|HighFragment",
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "in_boxcoord") 
                },
                new string[]
                {
                    "out_Colour",
                    "out_Normal",
                    "out_Shading",
                    "out_Lighting"
                });
            return program;
        }

        private void SetShader(ShaderProgram newprogram)
        {
            if (this.shader != null)
            {
                this.shader.Unload();
            }
            this.shader = newprogram;
        }

        public void Reload()
        {
            this.ReloadShader(this.LoadShader, this.SetShader, log);
        }


        public void Render(TerrainTile tile, TerrainGlobal terrainGlobal, Matrix4 projection, Matrix4 view, Vector3 eyePos)
        {
            var boxparam = tile.GetBoxParam();

            //Matrix4 transform = projection * view * tile.ModelMatrix;
            Matrix4 transform = tile.ModelMatrix * view * projection;

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);  // we only want to render front-faces

            tile.HeightTexture.Bind(TextureUnit.Texture0);
            tile.ParamTexture.Bind(TextureUnit.Texture1);
            tile.NormalTexture.Bind(TextureUnit.Texture2);
            terrainGlobal.ShadeTexture.Bind(TextureUnit.Texture3);
            terrainGlobal.TerrainDetailTexture.Bind(TextureUnit.Texture4);

            this.shader
                .UseProgram()
                .SetUniform("transform_matrix", transform)
                .SetUniform("heightTex", 0)
                .SetUniform("paramTex", 1)
                .SetUniform("normalTex", 2)
                .SetUniform("shadeTex", 3)
                .SetUniform("detailTex", 4)
                .SetUniform("eyePos", eyePos)
                .SetUniform("boxparam", boxparam)
                .SetUniform("patchSize", this.Width)
                .SetUniform("scale", this.Scale)
                .SetUniform("offset", this.Offset)
                .SetUniform("detailTexScale", this.DetailTexScale);
            this.mesh.Bind(this.shader.VariableLocation("vertex"), this.shader.VariableLocation("in_boxcoord"));
            this.mesh.Render();

        }

    }
}
