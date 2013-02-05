using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;
using Utils;
using TerrainGeneration;

namespace Snowscape.TerrainGenerationViewer
{
    public class TerrainGenerationViewer : GameWindow
    {
        const int TileWidth = 1024;
        const int TileHeight = 1024;

        public TerrainGen Terrain { get; set; }

        private Matrix4 projection = Matrix4.Identity;
        private Matrix4 modelview = Matrix4.Identity;
        private ShaderProgram quadShader = new ShaderProgram("QuadShader");
        private VBO quadVertexVBO = new VBO("quadvertex", BufferTarget.ArrayBuffer);
        private VBO quadTexcoordVBO = new VBO("quadtexcoord", BufferTarget.ArrayBuffer);
        private VBO quadIndexVBO = new VBO("quadindex", BufferTarget.ElementArrayBuffer);
        private Texture heightTex;
        private Texture shadeTex;
        float[] heightTexData = new float[TileWidth * TileHeight];
        byte[] shadeTexData = new byte[TileWidth * TileHeight * 4];
        uint frameCounter = 0;
        uint updateCounter = 0;
        private Font font = new Font();
        private TextManager textManager = new TextManager("DefaultText", null);


        private Vector3[] quadPos = new Vector3[]{
            new Vector3(0f,0f,0f),
            new Vector3(0f,1f,0f),
            new Vector3(1f,0f,0f),
            new Vector3(1f,1f,0f)
        };

        private Vector2[] quadTexCoord = new Vector2[]{
            new Vector2(0f,0f),
            new Vector2(0f,1f),
            new Vector2(1f,0f),
            new Vector2(1f,1f)
        };

        private uint[] quadIndex = new uint[] { 0, 1, 2, 3 };

        #region shaders
        private string vertexShaderSource = @"
#version 140
 
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

        private string fragmentShaderSource = @"
#version 140
precision highp float;

uniform sampler2D heightTex;
uniform sampler2D shadeTex;

in vec2 texcoord0;
out vec4 out_Colour;

const float TEXEL = 1.0 / 1024.0;

vec3 getNormal(vec2 pos)
{
    float h1 = texture2D(heightTex,vec2(pos.x,pos.y-TEXEL)).r;
    float h2 = texture2D(heightTex,vec2(pos.x,pos.y+TEXEL)).r;
    float h3 = texture2D(heightTex,vec2(pos.x-TEXEL,pos.y)).r;
    float h4 = texture2D(heightTex,vec2(pos.x+TEXEL,pos.y)).r;

    return normalize(vec3(h4-h3,h2-h1,2.0*TEXEL));
}


void main(void)
{

	vec4 colH1 = vec4(0.3,0.247,0.223,1.0);
	vec4 colH2 = vec4(0.3,0.247,0.223,1.0);

	vec4 colL1 = vec4(0.41,0.39,0.16,1.0);
	vec4 colL2 = vec4(0.41,0.39,0.16,1.0);

	vec4 colW = vec4(0.7,0.8,1.0,1.0);
	vec4 colE = vec4(1.0,0.4,0.0,1.0);

	vec4 s = texture2D(shadeTex,texcoord0.st);
	float h = texture2D(heightTex,texcoord0.st).r;

	float looseblend = clamp(s.g * s.g * 8.0,0.0,1.0);
	vec4 col = mix(mix(colH1,colH2,h),mix(colL1,colL2,h),looseblend);

	vec4 colW0 = vec4(0.325,0.498,0.757,1.0);  // blue water
	vec4 colW1 = vec4(0.659,0.533,0.373,1.0);  // dirty water
	vec4 colW2 = vec4(1.4,1.4,1.4,1.0); // white water

	colW = mix(colW0,colW1,clamp(s.r*4.0,0,1));  // make water dirty->clean
	colW = mix(colW,colW2,s.a*0.2);  // white water

	col = mix(col,colE,clamp(s.a,0.0,0.2));
	col = mix(col,colW,clamp(s.b*s.b*16.0,0,0.6)); // water

    vec3 n = getNormal(texcoord0.st);
    vec3 l = normalize(vec3(0.4,0.6,0.2));

	float diffuse = clamp(dot(n,l) * 0.5 + 0.5,0,1);
	col *= (0.4 + 0.6 * diffuse);

    out_Colour = vec4(col.rgb,1.0);
  
}
        ";
        #endregion

        public class CloseEventArgs : EventArgs { }
        public delegate void CloseEventHandler(object source, CloseEventArgs e);
        public event CloseEventHandler OnClose;

        public TerrainGenerationViewer()
            : base(640, 480, new GraphicsMode(), "Snowscape", GameWindowFlags.Default, DisplayDevice.Default, 3, 1, GraphicsContextFlags.Default)
        {
            this.Terrain = new TerrainGen(1024, 1024);
        }

        protected override void OnClosed(EventArgs e)
        {
            OnClose(this, new CloseEventArgs());
            base.OnClosed(e);
        }


        protected override void OnLoad(EventArgs e)
        {
            this.VSync = VSyncMode.On;

            // create VBOs/Shaders etc

            // GL state
            GL.Enable(EnableCap.DepthTest);

            this.heightTex = new Texture("height", TileWidth, TileHeight, TextureTarget.Texture2D, PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float);
            this.heightTex
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat))
                .Upload(heightTexData);

            this.shadeTex = new Texture("shade", TileWidth, TileHeight, TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);
            this.shadeTex
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat))
                .Upload(shadeTexData);


            // setup VBOs
            this.quadVertexVBO.SetData(this.quadPos);
            this.quadTexcoordVBO.SetData(this.quadTexCoord);
            this.quadIndexVBO.SetData(this.quadIndex);

            // setup shader
            quadShader.Init(this.vertexShaderSource, this.fragmentShaderSource, new List<Variable> { new Variable(0, "vertex"), new Variable(1, "in_texcoord0") });

            // setup font
            font.Init(Resources.FontConsolas, Resources.FontConsolasMeta);
            //font.Init(Resources.FontSegoeScript, Resources.FontSegoeScriptMeta);
            //font.Init(Resources.FontOCR, Resources.FontOCRMeta);
            textManager.Font = font;

            //font.AddChar('A', 0.2f, 0.1f, 0.0f, 0.003f);
            //font.AddChar('b', 0.3f, 0.1f, 0.0f, 0.003f);
            //font.AddChar('°', 0.4f, 0.1f, 0.0f, 0.003f);
            //font.AddString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 0.1f, 0.1f, 0.0f, 0.003f, new Vector4(1.0f, 0.5f, 0.0f, 1.0f));
            //font.AddString("abcdefghijklmnopqrstuvwxyz", 0.1f, 0.25f, 0.0f, 0.003f, new Vector4(0.3f, 0.5f, 1.0f, 1.0f));
            //font.Refresh();

            textManager.Add(new TextBlock("l1", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", new Vector3(0.05f, 0.1f, 0.0f), 0.003f, new Vector4(1.0f, 0.5f, 0.0f, 1.0f)));
            textManager.Add(new TextBlock("l2", "abcdefghijklmnopqrstuvwxyz", new Vector3(0.05f, 0.25f, 0.0f), 0.002f, new Vector4(1.0f, 0.2f, 0.6f, 1.0f)));
            textManager.Add(new TextBlock("l3", "0123456789!@#$%^&*()-=_+[]{}", new Vector3(0.05f, 0.3f, 0.0f), 0.0005f, new Vector4(0.1f, 0.6f, 1.0f, 1.0f)));

            SetProjection();

            // slow - replace with load
            this.Terrain.InitTerrain1();



            base.OnLoad(e);
        }


        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);
        }

        protected override void OnResize(EventArgs e)
        {
            SetProjection();
            base.OnResize(e);
        }

        private void SetProjection()
        {
            GL.Viewport(this.ClientRectangle);

            this.projection = Matrix4.CreateOrthographicOffCenter(0.0f, (float)this.ClientRectangle.Width / (float)this.ClientRectangle.Height, 1.0f, 0.0f, 0.001f, 10.0f);
            this.modelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);

        }

        protected void UpdateHeightTexture()
        {
            ParallelHelper.For2D(TileWidth, TileHeight, (x, y, i) =>
            {
                this.heightTexData[i] = (this.Terrain.Map[i].Height) / 4096.0f;
            });
            this.heightTex.RefreshImage(this.heightTexData);
        }

        protected void UpdateShadeTexture()
        {
            ParallelHelper.For2D(TileWidth, TileHeight, (x, y, i) =>
            {
                int j = i << 2;

                this.shadeTexData[j] = (byte)((this.Terrain.Map[i].Loose * 4.0f).ClampInclusive(0.0f, 255.0f));
                this.shadeTexData[j + 1] = (byte)((this.Terrain.Map[i].MovingWater * 2048.0f).ClampInclusive(0.0f, 255.0f));
                this.shadeTexData[j + 2] = (byte)((this.Terrain.Map[i].Erosion * 32f).ClampInclusive(0.0f, 255.0f));  // erosion rate
                this.shadeTexData[j + 3] = (byte)((this.Terrain.Map[i].Carrying * 32f).ClampInclusive(0.0f, 255.0f)); // carrying capacity
            });
            this.shadeTex.RefreshImage(this.shadeTexData);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            /*
            if (updateCounter % 3 == 0)
            {
                this.Terrain.ModifyTerrain();
            }*/

            updateCounter++;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {

            if (frameCounter % 200 == 0)
            {
                UpdateShadeTexture();
            }
            if (frameCounter % 200 == 100)
            {
                UpdateHeightTexture();
            }

            GL.ClearColor(new Color4(0, 96, 64, 255));
            GL.ClearDepth(10.0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            this.heightTex.Bind(TextureUnit.Texture0);
            this.shadeTex.Bind(TextureUnit.Texture1);
            quadShader.UseProgram();
            quadShader.SetUniform("projection_matrix", this.projection);
            quadShader.SetUniform("modelview_matrix", this.modelview);
            quadShader.SetUniform("heightTex", 0);
            quadShader.SetUniform("shadeTex", 1);
            quadVertexVBO.Bind(quadShader.VariableLocation("vertex"));
            quadTexcoordVBO.Bind(quadShader.VariableLocation("in_texcoord0"));
            quadIndexVBO.Bind();

            GL.DrawElements(BeginMode.TriangleStrip, quadIndexVBO.Length, DrawElementsType.UnsignedInt, 0);

            GL.Disable(EnableCap.DepthTest);
            //font.Render(projection, modelview);
            textManager.Render(projection, modelview);
            GL.Enable(EnableCap.DepthTest);

            GL.Flush();

            SwapBuffers();

            frameCounter++;
        }

    }
}
