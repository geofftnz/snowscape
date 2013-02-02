using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;

namespace Snowscape.TerrainGenerationViewer
{
    public class TerrainGenerationViewer : GameWindow
    {
        private int heightTex = 0;

        private Matrix4 projection = Matrix4.Identity;
        private Matrix4 modelview = Matrix4.Identity;
        private ShaderProgram quadShader = new ShaderProgram();
        private VBO quadVertexVBO = new VBO(BufferTarget.ArrayBuffer);
        private VBO quadIndexVBO = new VBO(BufferTarget.ElementArrayBuffer);


        private Vector3[] quadPos = new Vector3[]{
            new Vector3(0f,0f,0f),
            new Vector3(0f,1f,0f),
            new Vector3(1f,0f,0f),
            new Vector3(1f,1f,0f)
        };

        private uint[] quadIndex = new uint[] { 0,1,2,3 };
        
        private string vertexShaderSource = @"
#version 140
 
uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
in vec3 vertex;
 
void main() {
    gl_Position = projection_matrix * modelview_matrix * vec4(vertex, 1.0);
}
        ";

        private string fragmentShaderSource = @"
#version 140
precision highp float;

out vec4 out_Color;

void main(void)
{
    out_Color = vec4(1.,0.5,0.,1.);
}

        ";

        public class CloseEventArgs : EventArgs { }
        public delegate void CloseEventHandler(object source, CloseEventArgs e);
        public event CloseEventHandler OnClose;

        public TerrainGenerationViewer()
            : base(640, 480, new GraphicsMode(), "Snowscape", GameWindowFlags.Default, DisplayDevice.Default, 3, 1, GraphicsContextFlags.Default)
        {

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

            this.heightTex = GL.GenTexture();

            byte[] image = new byte[1024*1024*4];

            int i = 0;
            for (int y = 0; y < 1024; y++)
            {
                for (int x = 0; x < 1024; x++)
                {
                    image[i++] = (byte)(y & 0xff);
                    image[i++] = (byte)(x & 0xff);
                    image[i++] = (byte)((x*y) & 0xff);
                    image[i++] = 255;
                }
            }

            GL.BindTexture(TextureTarget.Texture2D,this.heightTex);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1024, 1024, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image);

            // setup VBOs
            this.quadVertexVBO.SetData(this.quadPos);
            this.quadIndexVBO.SetData(this.quadIndex);

            // setup shader
            quadShader.Init(this.vertexShaderSource, this.fragmentShaderSource, new List<Variable> { new Variable(0,"vertex") });

            SetProjection();

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

            this.projection = Matrix4.CreateOrthographicOffCenter(0.0f,(float)this.ClientRectangle.Width / (float)this.ClientRectangle.Height, 1.0f, 0.0f,0.001f, 10.0f);
            this.modelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f,0.0f,-1.0f);

        }


        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            //base.OnUpdateFrame(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.ClearColor(new Color4(0, 96, 64, 255));
            GL.ClearDepth(10.0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            quadShader.UseProgram();
            quadShader.SetUniform("projection_matrix", this.projection);
            quadShader.SetUniform("modelview_matrix", this.modelview);
            quadVertexVBO.Bind(quadShader.VariableLocation("vertex"));
            quadIndexVBO.Bind();

            GL.DrawElements(BeginMode.TriangleStrip, quadIndexVBO.Length, DrawElementsType.UnsignedInt, 0);

            GL.Flush();

            SwapBuffers();
        }

    }
}
