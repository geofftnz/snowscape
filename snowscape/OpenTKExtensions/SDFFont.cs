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

        public bool IsTextureLoaded { get; private set; }
        public bool IsVertexVBOLoaded { get; private set; }
        public bool IsIndexVBOLoaded { get; private set; }
        public bool IsTexcoordVBOLoaded { get; private set; }
        public bool IsLoaded
        {
            get
            {
                return this.IsTextureLoaded && this.IsVertexVBOLoaded && this.IsIndexVBOLoaded && this.IsTexcoordVBOLoaded;
            }
        }

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
                
                float t = texture2D(tex0,texcoord0.st).a;

                vec4 col = vec4(1.0,1.0,1.0,1.0);

                if (t < 0.5){
                    col.a = 0.0;
                }   
                else{
                    col.a = 1.0;
                }

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
            var data = fileName.LoadImage(out info).ExtractChannelFromRGBA(0);

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
                uint c6 = (uint)c * 6;

                index[i++] = c6 + 0;
                index[i++] = c6 + 1;
                index[i++] = c6 + 3;
                index[i++] = c6 + 1;
                index[i++] = c6 + 2;
                index[i++] = c6 + 3;
            }

            this.indexVBO = new VBO(BufferTarget.ElementArrayBuffer, BufferUsageHint.StaticDraw);
            this.indexVBO.SetData(index);
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
        }

        public void Init()
        {
        }

    }
}
