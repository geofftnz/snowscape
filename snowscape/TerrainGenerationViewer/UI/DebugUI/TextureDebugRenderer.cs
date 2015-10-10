using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;
using OpenTKExtensions.Framework;
using OpenTKExtensions.Text;

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
            /// R, scaled
            /// </summary>
            Height,

            GradientGrey,
            GradientCol,
            GradientGreen,
            GradientBlue,

            /// <summary>
            /// DO NOT USE - used to mark the end of the enum.
            /// </summary>
            MAX
        }

        public class TextureInfo
        {
            public string Source { get; set; }
            public Texture Texture { get; set; }
            public RenderMode RenderMode { get; set; }
            public float Scale { get; set; }
            public TextureInfo()
            {
                Scale = 1.0f;
                Source = "unknown";
                RenderMode = TextureDebugRenderer.RenderMode.RGB;
            }
        }

        private List<TextureInfo> textures = new List<TextureInfo>();
        public List<TextureInfo> Textures
        {
            get { return textures; }
        }

        public Font Font
        {
            get
            {
                return textManager.Font;
            }
            set
            {
                textManager.Font = value;
            }
        }

        // Needs:
        protected VBO vertexVBO = new VBO("v");
        protected VBO texcoordVBO = new VBO("t");
        protected VBO indexVBO = new VBO("I", BufferTarget.ElementArrayBuffer);
        protected Matrix4 transform = Matrix4.Identity;

        // text
        protected Font font;
        protected TextManager textManager = new TextManager("debugtexture-text", null);

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
            transform = Matrix4.Mult(Matrix4.CreateOrthographicOffCenter(0.0f, 1.0f, 1.0f, 0.0f, 0.0001f, 10.0f), Matrix4.CreateTranslation(0.0f, 0.0f, 1.0f));

            vsSource = @"
                #version 140

                uniform mat4 transform;

                in vec3 vertex;
                in vec2 in_texcoord;

                out vec2 texcoord;

                void main() {
	                gl_Position = transform * vec4(vertex,1.0);
	                texcoord = in_texcoord;
                }
                ";
            fsSource = @"
                #version 140

                uniform sampler2D tex;
                uniform int renderMode;
                uniform float scale;

                in vec2 texcoord;

                out vec4 out_Colour;

                float c255 = 1.0/255.0;

                vec3 gc(float r,float g,float b){
                    return vec3(r,g,b) * c255;
                }

                vec3 getHeightCol(float h){

                    vec3 c = mix(gc(24,53,27),gc(161,173,116),clamp(h,0.0,100.0) / 100.0);
                    c = mix(c,gc(224,168,152),clamp(h-100,0.0,100.0) / 100.0);
                    c = mix(c,gc(229,229,229),clamp(h-200,0.0,100.0) / 100.0);

                    return c;
                }

                vec3 getGradientCol(vec2 pos, float t, vec3 col){

                    float h1 = texture(tex,vec2(pos.x, pos.y - t)).r;
	                float h2 = texture(tex,vec2(pos.x, pos.y + t)).r;
                    float h3 = texture(tex,vec2(pos.x - t, pos.y)).r;
	                float h4 = texture(tex,vec2(pos.x + t, pos.y)).r;
	                vec3 n = normalize(vec3(h3-h4,2.0,h1-h2));

                    vec3 l = normalize(vec3(-0.5,0.5,-0.5));
                    return col * (dot(n,l) * 0.5 + 0.5);
                }
                vec3 getGradientColG(vec2 pos, float t){

                    float h1 = texture(tex,vec2(pos.x, pos.y - t)).g;
	                float h2 = texture(tex,vec2(pos.x, pos.y + t)).g;
                    float h3 = texture(tex,vec2(pos.x - t, pos.y)).g;
	                float h4 = texture(tex,vec2(pos.x + t, pos.y)).g;
	                vec3 n = normalize(vec3(h3-h4,2.0,h1-h2));

                    vec3 l = normalize(vec3(-0.5,0.5,-0.5));
                    return vec3(0.2,1.0,0.05) * (dot(n,l) * 0.5 + 0.5);
                }
                vec3 getGradientColB(vec2 pos, float t){

                    float h1 = texture(tex,vec2(pos.x, pos.y - t)).b;
	                float h2 = texture(tex,vec2(pos.x, pos.y + t)).b;
                    float h3 = texture(tex,vec2(pos.x - t, pos.y)).b;
	                float h4 = texture(tex,vec2(pos.x + t, pos.y)).b;
	                vec3 n = normalize(vec3(h3-h4,2.0,h1-h2));

                    vec3 l = normalize(vec3(-0.5,0.5,-0.5));
                    return vec3(0.2,0.7,1.0) * (dot(n,l) * 0.5 + 0.5);
                }

                void main(void) {

                    vec4 t = texture(tex,texcoord);
                    vec3 c = vec3(0.0,0.0,0.0);

                    t *= scale;

                    switch(renderMode)
                    {
                        case 0: c = t.rgb; break;
                        case 1: c = vec3(t.r) * vec3(1.0,0.8,0.8); break;
                        case 2: c = vec3(t.g) * vec3(0.8,1.0,0.8); break;
                        case 3: c = vec3(t.b) * vec3(0.8,0.8,1.0); break;
                        case 4: c = vec3(t.a); break;
                        case 5: c = t.rbg * 0.5 + 0.5; break;  // normal
                        case 6: c = getHeightCol(t.r); break;
                        case 7: c = getGradientCol(texcoord,1.0/1024.0,vec3(0.9)); break;  // gradient-grey
                        case 8: c = getGradientCol(texcoord,1.0/1024.0,getHeightCol(t.r)); break; // gradient-col
                        case 9: c = getGradientColG(texcoord,1.0/1024.0); break;  // gradient-green channel
                        case 10: c = getGradientColB(texcoord,1.0/1024.0); break;  // gradient-blue channel
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

        private RenderMode GuessRenderMode(Texture t)
        {
            if (t.Name.IndexOf("height", StringComparison.OrdinalIgnoreCase) != -1) return RenderMode.Height;
            //if (t.Name.IndexOf("normal", StringComparison.OrdinalIgnoreCase) != -1) return RenderMode.Normal;

            if (t.Format == PixelFormat.Red) return RenderMode.R;

            return RenderMode.RGB;
        }



        public void Add(Texture texture, object source, RenderMode renderMode = RenderMode.RGB)
        {
            if (!textures.Any(ti => ti.Texture == texture))
            {
                textures.Add(new TextureInfo { Texture = texture, RenderMode = renderMode, Source = source.GetType().Name });
                currentTexture = textures.Count - 1;
            }
        }
        public void Add(Texture texture, object source)
        {
            Add(texture, source, GuessRenderMode(texture));
        }

        public void Add(IEnumerable<Tuple<Texture, object>> textures)
        {
            foreach (var texture in textures)
            {
                Add(texture.Item1, texture.Item2);
            }
        }

        public int Previous()
        {
            if (currentTexture > 0) currentTexture--;
            return currentTexture;
        }
        public int Next()
        {
            if (currentTexture >= 0 && currentTexture < this.Textures.Count - 1) currentTexture++;
            return currentTexture;
        }

        public RenderMode PreviousRenderMode()
        {
            if (currentTexture < 0) return RenderMode.RGB;
            if (Textures[currentTexture].RenderMode == (RenderMode)0) return Textures[currentTexture].RenderMode;
            return Textures[currentTexture].RenderMode = (RenderMode)(((int)Textures[currentTexture].RenderMode + (int)RenderMode.MAX - 1) % (int)RenderMode.MAX);
        }
        public RenderMode NextRenderMode()
        {
            if (currentTexture < 0) return RenderMode.RGB;
            if (Textures[currentTexture].RenderMode == (RenderMode)((int)RenderMode.MAX - 1)) return Textures[currentTexture].RenderMode;
            return Textures[currentTexture].RenderMode = (RenderMode)(((int)Textures[currentTexture].RenderMode + 1) % (int)RenderMode.MAX);
        }

        public float ScaleDown()
        {
            if (currentTexture < 0) return 1.0f;
            var t = Textures[currentTexture];

            t.Scale *= 0.5f;
            return t.Scale;
        }
        public float ScaleUp()
        {
            if (currentTexture < 0) return 1.0f;
            var t = Textures[currentTexture];

            t.Scale *= 2.0f;
            return t.Scale;
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

            GL.Disable(EnableCap.CullFace);
            //GL.CullFace(CullFaceMode.Back);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);


            texture.Texture.Bind(TextureUnit.Texture0);
            program
                .UseProgram()
                .SetUniform("transform", transform)
                .SetUniform("tex", 0)
                .SetUniform("renderMode", (int)texture.RenderMode)
                .SetUniform("scale", texture.Scale);

            this.vertexVBO.Bind(program.VariableLocation("vertex"));
            this.texcoordVBO.Bind(program.VariableLocation("in_texcoord"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);

            this.RenderText();

        }

        private void RenderText()
        {
            float x = 0.2f;
            float y = 0.1f;
            float size = 0.0004f;
            float alpha = 0.7f;

            if (this.currentTexture >= 0)
            {
                textManager.AddOrUpdate(new TextBlock("textureinfo",

                    string.Format(
                        "{0}: {1} ({2}) {3} scale:{4:0.000}",
                        Textures[currentTexture].Source,
                        Textures[currentTexture].Texture.Name,
                        Textures[currentTexture].RenderMode.ToString(),
                        Textures[currentTexture].Texture.InternalFormat.ToString(),
                        Textures[currentTexture].Scale
                        ),

                    new Vector3(x, y, 0.0f), size, new Vector4(1f, 1f, 1f, alpha)));
                y += size * 1.2f;
            }

            if (textManager.NeedsRefresh)
            {
                textManager.Refresh();
            }

            GL.Enable(EnableCap.Blend);
            textManager.Projection = transform;
            textManager.Modelview = Matrix4.Identity;

            textManager.Render();
        }

    }
}




