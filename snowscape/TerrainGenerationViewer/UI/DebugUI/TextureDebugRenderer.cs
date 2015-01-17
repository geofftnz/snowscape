using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainGenerationViewer.UI.Debug
{
    public class TextureDebugRenderer : GameComponentBase, IRenderable
    {
        private const string Name = "TextureDebugRenderer";

        public enum RenderMode
        {
            RGB = 0,
            R,
            G,
            B,
            A,
            /// <summary>
            /// RGB, normalized around 0,0,0
            /// </summary>
            Normal,

            /// <summary>
            /// DO NOT USE - used to mark the end of the enum.
            /// </summary>
            MAX
        }

        public class TextureInfo
        {
            public Texture Texture { get; set; }
            public RenderMode RenderMode { get; set; }
            public TextureInfo()
            {
            }
        }

        private List<TextureInfo> textures = new List<TextureInfo>();
        public List<TextureInfo> Textures
        {
            get { return textures; }
        }

        // Needs:
        protected VBO vertexVBO = new VBO("v");
        protected VBO texcoordVBO = new VBO("t");
        protected VBO indexVBO = new VBO("I",BufferTarget.ElementArrayBuffer);

        private string vsSource = "";
        private string fsSource = "";
        protected ShaderProgram program;

        private int currentTexture = -1;


        public TextureDebugRenderer()
        {
            Visible = true;
            DrawOrder = 0;

            this.Loading += TextureDebugRenderer_Loading;
        }

        void TextureDebugRenderer_Loading(object sender, EventArgs e)
        {
            vsSource = @"
                #version 140

                in vec3 vertex;
                in vec2 in_texcoord;

                out vec2 texcoord;

                void main() {
	                gl_Position = vec4(vertex.xy,0.0,1.0);
	                texcoord = in_texcoord;
                }
                ";
            fsSource = @"
                #version 140

                uniform sampler2D tex;
                uniform int renderMode;

                in vec2 texcoord;

                out vec4 out_Colour;

                void main(void) {

                    vec4 t = texture(tex,texcoord);
                    vec3 c = vec3(0.0,0.0,0.0);

                    switch(renderMode)
                    {
                        case 0: c = t.rgb; break;
                        case 1: c = vec3(t.r); break;
                        case 2: c = vec3(t.g); break;
                        case 3: c = vec3(t.b); break;
                        case 4: c = vec3(t.a); break;
                        case 5: c = t.rgb * 0.5 + 0.5; break;  // normal
                    }

                    out_Colour = vec4(c,1.0);
                }
                ";

            this.InitVBOs();
            this.ReloadShader();
        }

        private void InitVBOs()
        {
            this.vertexVBO.SetData(new Vector3[] {
                                    new Vector3(0f,1f,0f),
                                    new Vector3(0f,0f,0f),
                                    new Vector3(1f,1f,0f),
                                    new Vector3(1f,0f,0f)
                                    });
            this.texcoordVBO.SetData(new Vector2[] {
                                    new Vector2(0f,1f),
                                    new Vector2(0f,0f),
                                    new Vector2(1f,1f),
                                    new Vector2(1f,0f)
                                });
            this.indexVBO.SetData(new uint[] {
                                0,1,2,
                                1,3,2
                            });
        }


        private ShaderProgram LoadShaderProgram()
        {
            var program = new ShaderProgram(Name);

            // setup shader
            program.Init(
                vsSource,
                fsSource,
                new List<Variable> 
                { 
                    new Variable(0, "vertex"),
                    new Variable(1, "in_texcoord")
                },
                new string[] { "out_Colour" },
                null
                );

            return program;
        }

        public bool ReloadShader()
        {
            try
            {
                ShaderProgram p = LoadShaderProgram();

                if (p == null)
                {
                    throw new InvalidOperationException("ReloadShader() returned null, but didn't throw an exception");
                }

                if (this.program != null)
                {
                    this.program.Unload();
                }
                this.program = p;
                return true;
            }
            catch (Exception ex)
            {
                if (log != null)
                {
                    log.Warn("Could not reload shader program {0}: {1}", Name, ex.GetType().Name + ": " + ex.Message);
                }
            }
            return false;

        }



        public void Add(Texture texture, RenderMode renderMode = RenderMode.RGB)
        {
            if (!textures.Any(ti => ti.Texture == texture))
            {
                textures.Add(new TextureInfo { Texture = texture, RenderMode = renderMode });
                currentTexture = textures.Count - 1;
            }
        }

        public void Add(IEnumerable<Texture> textures)
        {
            foreach (var texture in textures)
            {
                Add(texture);
            }
        }

        public bool Visible { get; set; }
        public int DrawOrder { get; set; }

        public void Render(IFrameRenderData frameData)
        {
            if (currentTexture < 0) return;
            if (currentTexture >= this.Textures.Count)
            {
                throw new IndexOutOfRangeException(string.Format("currentTexture ({0})", currentTexture));
            };

            var texture = this.Textures[currentTexture];
            if (texture == null || texture.Texture == null)
            {
                throw new InvalidOperationException("empty/null textureinfo");
            }

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.Disable(EnableCap.DepthTest);


            texture.Texture.Bind(TextureUnit.Texture0);
            program
                .UseProgram()
                .SetUniform("tex", 0)
                .SetUniform("renderMode", (int)texture.RenderMode);

            this.vertexVBO.Bind(program.VariableLocation("vertex"));
            this.texcoordVBO.Bind(program.VariableLocation("in_texcoord"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);

        }
    }
}




