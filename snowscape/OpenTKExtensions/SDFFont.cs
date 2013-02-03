using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace OpenTKExtensions
{
    /*
     * Signed Distance Field Font Rendering
     * 
     * Based one work by:
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
    public class SDFFont
    {
        const int MAXCHARS = 4000;
        const int NUMVERTICES = MAXCHARS * 4;
        const int NUMINDICES = MAXCHARS * 6;

        public bool IsTextureLoaded{ get; private set; }
        public bool IsVertexVBOLoaded { get; private set; }
        public bool IsIndexVBOLoaded { get; private set; }
        public bool IsTexcoordVBOLoaded { get; private set; }
        public bool IsShaderLoaded { get; private set; }
        public bool IsLoaded
        {
            get
            {
                return this.IsTextureLoaded && this.IsVertexVBOLoaded && this.IsIndexVBOLoaded && this.IsTexcoordVBOLoaded && this.IsShaderLoaded;
            }
        }
        public int TexWidth { get; private set; }
        public int TexHeight { get; private set; }

        private Texture sdfTexture;

        private Vector3[] vertex;
        private VBO vertexVBO;

        private Vector2[] texcoord;
        private VBO texcoordVBO;

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

        #region shaders
        private const string vertexShaderSource = 
            @"#version 140
 
            uniform mat4 projection_matrix;
            uniform mat4 modelview_matrix;
            in vec3 vertex;
            in vec2 in_texcoord0;
            out vec2 texcoord0;
 
            void main() {
                gl_Position = projection_matrix * modelview_matrix * vec4(vertex, 1.0);
                texcoord0 = in_texcoord0;
            }
            ";
        private const string fragmentShaderSource =
            @"#version 140
            precision highp float;

            uniform sampler2D tex0;

            in vec2 texcoord0;
            out vec4 out_Colour;

            void main() {
                
                float t = texture2D(tex0,texcoord0.xy).a;

                vec4 col = vec4(1.0,1.0,1.0,smoothstep(0.4,0.6,t));

                out_Colour = col;
            }
             ";

        #endregion


        public SDFFont()
        {
            this.IsTextureLoaded = false;
            this.IsVertexVBOLoaded = false;
            this.IsTexcoordVBOLoaded= false;
            this.IsIndexVBOLoaded = false;
            this.IsShaderLoaded = false;
            this.Count = 0;

        }

        public void LoadMetaData(string fileName)
        {
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
        }

        public void LoadTexture(string fileName)
        {
            ImageLoader.ImageInfo info;

            // load red channel from file.
            var data = fileName.LoadImage(out info).ExtractChannelFromRGBA(3);

            this.TexWidth = info.Width;
            this.TexHeight = info.Height;

            // setup texture

            this.sdfTexture = new Texture(info.Width,info.Height,TextureTarget.Texture2D,PixelInternalFormat.Alpha,PixelFormat.Alpha,PixelType.UnsignedByte);
            this.sdfTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp))
                .Upload(data);

            IsTextureLoaded = true;

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

            this.indexVBO = new VBO(BufferTarget.ElementArrayBuffer, BufferUsageHint.StaticDraw);
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

            this.vertexVBO = new VBO(BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw);
            this.vertexVBO.SetData(this.vertex);
            this.IsVertexVBOLoaded = true;
        }

        public void InitTexcoordVBO()
        {
            this.texcoord = new Vector2[NUMVERTICES];

            for (int i = 0; i < NUMVERTICES; i++)
            {
                this.texcoord[i] = Vector2.Zero;
            }

            this.texcoordVBO = new VBO(BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw);
            this.texcoordVBO.SetData(this.texcoord);
            this.IsTexcoordVBOLoaded = true;
        }

        public void Refresh()
        {
            this.vertexVBO.SetData(this.vertex);
            this.texcoordVBO.SetData(this.texcoord);
        }

        public void InitShader()
        {
            this.shader = new ShaderProgram();

            this.shader.Init(
                vertexShaderSource,
                fragmentShaderSource,
                new List<Variable>
                {
                    new Variable(0,"vertex"),
                    new Variable(1, "in_texcoord0")
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

        /// <summary>
        /// adds a character to the render list, returns cursor advance amount
        /// </summary>
        /// <param name="c"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public float AddChar(char c, float x, float y, float z, float size)
        {
            FontCharacter charinfo;

            if (this.Count < MAXCHARS && this.Characters.TryGetValue(c, out charinfo))
            {

                int i = this.Count * 4;  // offset into vertex VBO data

                // top left
                this.vertex[i].X = x + (charinfo.XOffset * size);
                this.vertex[i].Y = y;
                this.vertex[i].Z = z;
                this.texcoord[i] = charinfo.TexTopLeft;
                i++;

                // top right
                this.vertex[i].X = x + (charinfo.XOffset + charinfo.Width) * size;
                this.vertex[i].Y = y;
                this.vertex[i].Z = z;
                this.texcoord[i] = charinfo.TexTopRight;
                i++;

                // bottom left
                this.vertex[i].X = x + (charinfo.XOffset * size);
                this.vertex[i].Y = y + charinfo.Height * size;
                this.vertex[i].Z = z;
                this.texcoord[i] = charinfo.TexBottomLeft;
                i++;

                // bottom right
                this.vertex[i].X = x + (charinfo.XOffset + charinfo.Width) * size;
                this.vertex[i].Y = y + charinfo.Height * size;
                this.vertex[i].Z = z;
                this.texcoord[i] = charinfo.TexBottomRight;

                this.Count++;

                return charinfo.XAdvance * size;
            }
            return 0f;
        }

        public void Init(string imageFilename, string metadataFilename)
        {
            this.LoadTexture(imageFilename);
            this.LoadMetaData(metadataFilename);
            this.NormalizeTexcoords();
            this.InitVertexVBO();
            this.InitTexcoordVBO();
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
            this.texcoordVBO.Bind(shader.VariableLocation("in_texcoord0"));
            this.indexVBO.Bind();

            GL.DrawElements(BeginMode.Triangles, this.Count*6, DrawElementsType.UnsignedInt, 0);


        }

    }
}
