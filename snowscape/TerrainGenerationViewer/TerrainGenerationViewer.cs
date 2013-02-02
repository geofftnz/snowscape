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
        private ShaderProgram quadShader = new ShaderProgram();
        private VBO quadVertexVBO = new VBO(BufferTarget.ArrayBuffer);
        private VBO quadTexcoordVBO = new VBO(BufferTarget.ArrayBuffer);
        private VBO quadIndexVBO = new VBO(BufferTarget.ElementArrayBuffer);
        private Texture heightTex;
        private Texture shadeTex;
        float[] heightTexData = new float[TileWidth * TileHeight];
        byte[] shadeTexData = new byte[TileWidth * TileHeight*4];
        uint framecounter = 0;


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

uniform sampler2D tex0;

in vec2 texcoord0;
out vec4 out_Colour;

const float TEXEL = 1.0 / 1024.0;

vec3 getNormal(vec2 pos)
{
    float h1 = texture2D(tex0,vec2(pos.x,pos.y-TEXEL)).r;
    float h2 = texture2D(tex0,vec2(pos.x,pos.y+TEXEL)).r;
    float h3 = texture2D(tex0,vec2(pos.x-TEXEL,pos.y)).r;
    float h4 = texture2D(tex0,vec2(pos.x+TEXEL,pos.y)).r;

    return normalize(vec3(h4-h3,h2-h1,2.0*TEXEL));
}


void main(void)
{
    vec3 n = getNormal(texcoord0.st);
    vec3 l = normalize(vec3(-0.4,-0.6,-0.2));

    vec3 col = vec3(0.6,0.6,0.65) * (dot(n,l) * 0.5f + 0.5f);

    out_Colour = vec4(col.rgb,1.0);
  
}

        ";

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


            

            int i = 0;
            float fx=0f,fy=0f;

            for (int y = 0; y < 1024; y++)
            {
                fx=0f;
                for (int x = 0; x < 1024; x++)
                {
                    heightTexData[i] = SimplexNoise.noise(fx, fy);
                    i++;
                    fx += (16.0f / 1024.0f);
                }
                fy+=(16.0f/1024.0f);
            }

            this.heightTex = new Texture(1024, 1024, TextureTarget.Texture2D, PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float);

            this.heightTex
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat))
                .Upload(heightTexData);

            // setup VBOs
            this.quadVertexVBO.SetData(this.quadPos);
            this.quadTexcoordVBO.SetData(this.quadTexCoord);
            this.quadIndexVBO.SetData(this.quadIndex);

            // setup shader
            quadShader.Init(this.vertexShaderSource, this.fragmentShaderSource, new List<Variable> { new Variable(0, "vertex"), new Variable(1, "in_texcoord0") });

            SetProjection();

            // slow
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

        protected void UpdateTextures()
        {
            ParallelHelper.For2D(1024, 1024, (x, y, i) =>
            {
                this.heightTexData[i] = (this.Terrain.Map[i].Height) / 4096.0f;
            });

            this.heightTex.RefreshImage(this.heightTexData);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            this.Terrain.ModifyTerrain();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {

            if (framecounter % 1000 == 0)
            {
                UpdateTextures();
            }

            GL.ClearColor(new Color4(0, 96, 64, 255));
            GL.ClearDepth(10.0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            this.heightTex.Bind(TextureUnit.Texture0);
            quadShader.UseProgram();
            quadShader.SetUniform("projection_matrix", this.projection);
            quadShader.SetUniform("modelview_matrix", this.modelview);
            quadShader.SetUniform("tex0", 0);
            quadVertexVBO.Bind(quadShader.VariableLocation("vertex"));
            quadTexcoordVBO.Bind(quadShader.VariableLocation("in_texcoord0"));
            quadIndexVBO.Bind();

            GL.DrawElements(BeginMode.TriangleStrip, quadIndexVBO.Length, DrawElementsType.UnsignedInt, 0);

            GL.Flush();

            SwapBuffers();

            framecounter++;
        }

    }
}
