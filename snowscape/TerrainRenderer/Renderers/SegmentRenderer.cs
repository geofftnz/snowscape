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
    /// Renderer for a segment of terrain
    /// 
    /// This renders a single segment of the terrain using a square, normalized mesh and distorting it into a radial mesh in the vertex shader.
    /// The resulting mesh will be translated by the eye.xz coordinates such that it is always centred on the camera.
    /// 
    /// This renderer differs from the regular patch renderers in that it doesn't render a subset of a single patch in a different location,
    /// instead it always renders the same patch centred on the viewer and offsets the terrain coordinates.
    /// 
    /// The intention is to provide a constant LoD without the nasty jumps in detail between adjacent patches. There should be no holes between adjacent segments.
    /// 
    /// Downsides are that the entire terrain must be renderable by the segment and there may be vertex-swimming artifacts on motion.
    /// 
    /// Render parameters:
    /// - eyePos (already have this)
    /// - angleOffset
    /// - angleExtent
    /// - radiusOffset
    /// - radiusExtent
    /// 
    /// </summary>
    public class SegmentRenderer : GameComponentBase, ITileRenderer, IPatchRenderer, IReloadable, ISegmentRenderer
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

        public SegmentRenderer(int width, int height, IPatchCache patchCache)
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

            this.Loading += SegmentRenderer_Loading;
        }

        void SegmentRenderer_Loading(object sender, EventArgs e)
        {
            Reload();
        }

        private ShaderProgram LoadShader()
        {
            var program = new ShaderProgram(this.GetType().Name);
            // setup shader
            program.Init(
                @"PatchRender.glsl|SegmentVertex",
                @"PatchRender.glsl|SegmentFragment",
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
            throw new InvalidOperationException("wrong render function");
        }

        public void Render(TerrainTile tile, TerrainGlobal terrainGlobal, Matrix4 projection, Matrix4 view, Vector3 eyePos, float angleOffset, float angleExtent, float radiusOffset, float radiusExtent)
        {
            var boxparam = tile.GetBoxParam();

            // undo view translation to centre mesh on viewer
            //Matrix4 transform = Matrix4.CreateTranslation(-eyePos.X, 0f, -eyePos.Z) * view * projection;
            Matrix4 transform = Matrix4.Identity * view * projection;

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);  // we only want to render front-faces

            tile.HeightTexture.Bind(TextureUnit.Texture0);
            tile.ParamTexture.Bind(TextureUnit.Texture1);
            tile.NormalTexture.Bind(TextureUnit.Texture2);
            terrainGlobal.ShadeTexture.Bind(TextureUnit.Texture3);
            terrainGlobal.TerrainDetailTexture.Bind(TextureUnit.Texture4);

            this.shader
                .UseProgram()
                .SetUniform("angleOffset", angleOffset)
                .SetUniform("angleExtent", angleExtent)
                .SetUniform("radiusOffset", radiusOffset)
                .SetUniform("radiusExtent", radiusExtent)
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
