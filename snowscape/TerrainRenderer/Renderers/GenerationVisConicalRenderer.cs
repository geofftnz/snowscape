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
    public class GenerationVisConicalRenderer : GameComponentBase, ITileRenderer
    {
        private ConicalMesh mesh;
        private ShaderProgram shader = new ShaderProgram("visconical");

        public int Width { get; private set; }
        public int Height { get; private set; }

        public float Scale { get; set; }
        public Vector2 Offset { get; set; }
        public float DetailScale { get; set; }
        public Texture DetailTexture { get; set; }
        public float DetailTexScale { get; set; }

        public GenerationVisConicalRenderer(int width, int height)
            : base()
        {
            if (width != height)
            {
                throw new InvalidOperationException("Patch must be square.");
            }
            this.Width = width;
            this.Height = height;
            this.Scale = 1.0f;
            this.Offset = Vector2.Zero;
            this.DetailScale = 1.0f;
            this.DetailTexScale = 0.1f;

            this.mesh = new ConicalMesh(width, height);

            this.Loading += GenerationVisConicalRenderer_Loading;
        }

        void GenerationVisConicalRenderer_Loading(object sender, EventArgs e)
        {
            this.mesh.Load();
            Reload();
        }


        private ShaderProgram LoadShader()
        {
            var program = new ShaderProgram(this.GetType().Name);
            // setup shader
            program.Init(
                @"PatchRender.glsl|ConicalVertex",
                @"PatchRender.glsl|ConicalFragment",
                new List<Variable> 
                { 
                    new Variable(0, "vertex")
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
            this.mesh.Bind(this.shader.VariableLocation("vertex"));
            this.mesh.Render();

        }

    }
}
