using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using NLog;

namespace OpenTKExtensions
{
    /*
     * Signed Distance Field Font Rendering
     * 
     * Based on work by:
     * 
     * Chris Green / Valve: Improved Alpha-Tested Magniﬁcation for Vector Textures and Special Effects
     * http://www.valvesoftware.com/publications/2007/SIGGRAPH2007_AlphaTestedMagnification.pdf
     * 
     * Lonesock (Font SDF generator)
     * http://www.gamedev.net/topic/491938-signed-distance-bitmap-font-tool/
     * 
     * 
     * Most OpenTK text renderers out there work on the basis of getting System.Drawing to do the hard work 
     * and then write the output to a texture. This one maintains VBOs of MAXCHARS
     * 
     * 
     * 
     * 
    */
    public class Font : ITextRenderer
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        const int MAXCHARS = 4000;
        const int NUMVERTICES = MAXCHARS * 4;
        const int NUMINDICES = MAXCHARS * 6;

        public string Name { get; set; }

        public bool IsTextureLoaded { get; private set; }
        public bool IsVertexVBOLoaded { get; private set; }
        public bool IsColourVBOLoaded { get; private set; }
        public bool IsIndexVBOLoaded { get; private set; }
        public bool IsTexcoordVBOLoaded { get; private set; }
        public bool IsShaderLoaded { get; private set; }
        public bool IsLoaded
        {
            get
            {
                return this.IsTextureLoaded && this.IsVertexVBOLoaded && this.IsIndexVBOLoaded && this.IsTexcoordVBOLoaded && this.IsShaderLoaded && this.IsColourVBOLoaded;
            }
        }
        public int TexWidth { get; private set; }
        public int TexHeight { get; private set; }

        private Texture sdfTexture;

        private Vector3[] vertex;
        private VBO vertexVBO;

        private Vector2[] texcoord;
        private VBO texcoordVBO;

        private Vector4[] colour;
        private VBO colourVBO;

        private VBO indexVBO;

        private ShaderProgram shader;

        private Dictionary<char, FontCharacter> characters = new Dictionary<char, FontCharacter>();
        public Dictionary<char, FontCharacter> Characters
        {
            get
            {
                return characters;
            }
        }

        public int Count { get; private set; }
        public float GlobalScale { get; set; }

        #region shaders
        private const string vertexShaderSource =
            @"#version 140
 
            uniform mat4 projection_matrix;
            uniform mat4 modelview_matrix;
            in vec3 vertex;
            in vec2 in_texcoord0;
            in vec4 in_col0;

            out vec2 texcoord0;
            out vec4 col0;
 
            void main() {
                gl_Position = projection_matrix * modelview_matrix * vec4(vertex, 1.0);
                texcoord0 = in_texcoord0;
                col0 = in_col0;
            }
            ";
        private const string fragmentShaderSource =
            @"#version 140
            precision highp float;

            uniform sampler2D tex0;

            in vec2 texcoord0;
            in vec4 col0;

            out vec4 out_Colour;

            void main() {
                float t = texture2D(tex0,texcoord0.xy).a;
                vec4 col = col0;
                col.a = col.a * smoothstep(0.4,0.6,t);
                out_Colour = col;
            }
             ";

        #endregion


        public Font(string name)
        {
            this.Name = name;
            this.IsTextureLoaded = false;
            this.IsVertexVBOLoaded = false;
            this.IsTexcoordVBOLoaded = false;
            this.IsIndexVBOLoaded = false;
            this.IsShaderLoaded = false;
            this.IsColourVBOLoaded = false;
            this.Count = 0;
            this.GlobalScale = 1.0f;
        }

        public Font()
            : this("Font")
        {

        }

        public void LoadMetaData(string fileName)
        {
            log.Trace("Font {0} loading meta-data from {1}", this.Name, fileName);
            using (var fs = new FileStream(fileName, FileMode.Open))
            {
                this.LoadMetaData(fs);
                fs.Close();
            }
        }
        public void LoadMetaData(Stream input)
        {
            this.Characters.Clear();

            using (var sr = new StreamReader(input))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    var tempChar = new FontCharacter();

                    if (FontMetaParser.TryParseCharacterInfoLine(line, out tempChar))
                    {
                        char key = (char)tempChar.ID;

                        if (!this.Characters.ContainsKey(key))
                        {
                            this.Characters.Add(key, tempChar);
                        }
                    }
                }
                sr.Close();
            }
            log.Trace("Font {0} meta data loaded. {1} characters parsed.", this.Name, this.Characters.Count);
        }

        public void LoadTexture(string fileName)
        {
            log.Trace("Font {0} loading texture from {1}", this.Name, fileName);

            ImageLoader.ImageInfo info;

            // load red channel from file.
            var data = fileName.LoadImage(out info).ExtractChannelFromRGBA(3);

            this.TexWidth = info.Width;
            this.TexHeight = info.Height;

            // setup texture

            this.sdfTexture = new Texture(this.Name + "_tex", info.Width, info.Height, TextureTarget.Texture2D, PixelInternalFormat.Alpha, PixelFormat.Alpha, PixelType.UnsignedByte);
            this.sdfTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp))
                .Upload(data);

            IsTextureLoaded = true;

            log.Trace("Font {0} texture loaded, resolution {1}x{2}", this.Name, this.TexWidth, this.TexHeight);
        }

        public void Unload()
        {
            if (this.sdfTexture != null && IsTextureLoaded)
            {
                this.sdfTexture.Unload();
                this.IsTextureLoaded = false;
            }
        }

        public void InitIndexVBO()
        {
            uint[] index = new uint[NUMINDICES];
            int i = 0;

            // indices are static. We'll move vertices and texcoords.
            for (int c = 0; c < MAXCHARS; c++)
            {
                uint c4 = (uint)c * 4;

                index[i++] = c4 + 0;
                index[i++] = c4 + 1;
                index[i++] = c4 + 2;
                index[i++] = c4 + 1;
                index[i++] = c4 + 3;
                index[i++] = c4 + 2;
            }

            this.indexVBO = new VBO(this.Name + "_index",BufferTarget.ElementArrayBuffer, BufferUsageHint.StaticDraw);
            this.indexVBO.SetData(index);
            this.IsIndexVBOLoaded = true;
        }

        public void InitVertexVBO()
        {
            this.vertex = new Vector3[NUMVERTICES];

            for (int i = 0; i < NUMVERTICES; i++)
            {
                this.vertex[i] = Vector3.Zero;
            }

            this.vertexVBO = new VBO(this.Name + "_vertex", BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw);
            this.vertexVBO.SetData(this.vertex);
            this.IsVertexVBOLoaded = true;
        }

        public void InitColourVBO()
        {
            this.colour = new Vector4[NUMVERTICES];

            for (int i = 0; i < NUMVERTICES; i++)
            {
                this.colour[i] = Vector4.One;
            }

            this.colourVBO = new VBO(this.Name + "_colour", BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw);
            this.colourVBO.SetData(this.colour);
            this.IsColourVBOLoaded = true;
        }

        public void InitTexcoordVBO()
        {
            this.texcoord = new Vector2[NUMVERTICES];

            for (int i = 0; i < NUMVERTICES; i++)
            {
                this.texcoord[i] = Vector2.Zero;
            }

            this.texcoordVBO = new VBO(this.Name + "_texcoord", BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw);
            this.texcoordVBO.SetData(this.texcoord);
            this.IsTexcoordVBOLoaded = true;
        }

        public void Refresh()
        {
            this.vertexVBO.SetData(this.vertex);
            this.colourVBO.SetData(this.colour);
            this.texcoordVBO.SetData(this.texcoord);
        }

        public void InitShader()
        {
            this.shader = new ShaderProgram(this.Name);

            this.shader.Init(
                vertexShaderSource,
                fragmentShaderSource,
                new List<Variable>
                {
                    new Variable(0, "vertex"),
                    new Variable(1, "in_texcoord0"),
                    new Variable(2, "in_col0")
                });

            this.IsShaderLoaded = true;
        }

        public void NormalizeTexcoords()
        {
            foreach (var c in this.Characters)
            {
                c.Value.NormalizeTexcoords((float)this.TexWidth, (float)this.TexHeight);
            }
        }

        public Vector3 MeasureChar(char c, float size)
        {
            FontCharacter charinfo;
            size *= GlobalScale;
            if (this.Characters.TryGetValue(c, out charinfo))
            {
                return new Vector3(charinfo.XAdvance * size, charinfo.Height * size, 0f);
            }
            return Vector3.Zero;
        }

        public Vector3 MeasureString(string s, float size)
        {
            Vector3 r = Vector3.Zero;

            foreach (char c in s)
            {
                var charsize = this.MeasureChar(c, size);
                r.X += charsize.X;
                if (r.Y < charsize.Y)
                {
                    r.Y = charsize.Y;
                }
                if (r.Z < charsize.Z)
                {
                    r.Z = charsize.Z;
                }

            }

            return r;
        }

        /// <summary>
        /// adds a character to the render list, returns cursor advance amount
        /// </summary>
        /// <param name="c"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public float AddChar(char c, float x, float y, float z, float size, Vector4 col)
        {
            FontCharacter charinfo;
            size *= GlobalScale;

            if (this.Count < MAXCHARS && this.Characters.TryGetValue(c, out charinfo))
            {

                int i = this.Count * 4;  // offset into vertex VBO data

                // top left
                this.vertex[i].X = x + (charinfo.XOffset * size);
                this.vertex[i].Y = y + (-charinfo.YOffset * size);
                this.vertex[i].Z = z;
                this.colour[i] = col;
                this.texcoord[i] = charinfo.TexTopLeft;
                i++;

                // top right
                this.vertex[i].X = x + (charinfo.XOffset + charinfo.Width) * size;
                this.vertex[i].Y = y + (-charinfo.YOffset * size);
                this.vertex[i].Z = z;
                this.colour[i] = col;
                this.texcoord[i] = charinfo.TexTopRight;
                i++;

                // bottom left
                this.vertex[i].X = x + (charinfo.XOffset * size);
                this.vertex[i].Y = y + (-charinfo.YOffset + charinfo.Height) * size;
                this.vertex[i].Z = z;
                this.colour[i] = col;
                this.texcoord[i] = charinfo.TexBottomLeft;
                i++;

                // bottom right
                this.vertex[i].X = x + (charinfo.XOffset + charinfo.Width) * size;
                this.vertex[i].Y = y + (-charinfo.YOffset + charinfo.Height) * size;
                this.vertex[i].Z = z;
                this.colour[i] = col;
                this.texcoord[i] = charinfo.TexBottomRight;

                this.Count++;

                return charinfo.XAdvance * size;
            }
            return 0f;
        }

        public float AddChar(char c, Vector3 position, float size, Vector4 col)
        {
            return AddChar(c, position.X, position.Y, position.Z, size, col);
        }

        public float AddString(string s, float x, float y, float z, float size, Vector4 col)
        {
            float xx = x;
            foreach (char c in s)
            {
                xx += AddChar(c, xx, y, z, size, col);
            }
            return xx - x;
        }

        public float AddString(string s, Vector3 position, float size, Vector4 col)
        {
            return AddString(s, position.X, position.Y, position.Z, size, col);
        }

        public void Init(string imageFilename, string metadataFilename)
        {
            this.LoadTexture(imageFilename);
            this.LoadMetaData(metadataFilename);
            this.NormalizeTexcoords();
            this.InitVertexVBO();
            this.InitTexcoordVBO();
            this.InitColourVBO();
            this.InitIndexVBO();
            this.InitShader();
        }

        public void Render(Matrix4 projection, Matrix4 modelview)
        {
            if (!this.IsLoaded)
            {
                throw new InvalidOperationException("Font isn't fully loaded.");
            }

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            this.sdfTexture.Bind(TextureUnit.Texture0);

            shader.UseProgram();
            shader.SetUniform("projection_matrix", projection);
            shader.SetUniform("modelview_matrix", modelview);
            shader.SetUniform("tex0", 0);
            this.vertexVBO.Bind(shader.VariableLocation("vertex"));
            this.colourVBO.Bind(shader.VariableLocation("in_col0"));
            this.texcoordVBO.Bind(shader.VariableLocation("in_texcoord0"));
            this.indexVBO.Bind();

            GL.DrawElements(BeginMode.Triangles, this.Count * 6, DrawElementsType.UnsignedInt, 0);

        }

        public void Clear()
        {
            this.Count = 0;
        }

    }
}
