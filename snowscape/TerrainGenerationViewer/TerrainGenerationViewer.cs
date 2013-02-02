﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Snowscape.TerrainGenerationViewer
{
    public class TerrainGenerationViewer : GameWindow
    {
        private int heightTex = 0;
        private int shaderProgram = 0;
        private int vertexShader = 0;
        private int fragmentShader = 0;

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
    out_Color = vec4(1.,1.,0.,1.);
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


            // setup shader
            this.vertexShader = GL.CreateShader(ShaderType.VertexShader);
            this.fragmentShader = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(this.vertexShader, this.vertexShaderSource);
            GL.ShaderSource(this.fragmentShader, this.fragmentShaderSource);

            GL.CompileShader(this.vertexShader);
            GL.CompileShader(this.fragmentShader);

            this.shaderProgram = GL.CreateProgram();
            GL.AttachShader(this.shaderProgram, this.vertexShader);
            GL.AttachShader(this.shaderProgram, this.fragmentShader);
            GL.LinkProgram(this.shaderProgram);

            GL.UseProgram(this.shaderProgram);
            
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
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0.0, (double)this.ClientRectangle.Width / (double)this.ClientRectangle.Height, 1.0, 0.0, 0.001, 10.0);
            GL.MatrixMode(MatrixMode.Modelview);
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

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Translate(0.0, 0.0, -1.0);
            //GL.Scale(0.5, 0.5, 0.5);

            //GL.Disable(EnableCap.Lighting);
            //GL.Disable(EnableCap.Texture2D);
            //GL.CullFace(CullFaceMode.Back);
            //GL.LineWidth(2.0f);

            //GL.Enable(EnableCap.Blend);
            //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Texture2D);

            GL.Begin(BeginMode.TriangleStrip);

            GL.Color4(0.0f, 0.0f, 0.5f, 1.0f);
            GL.TexCoord2(0.0, 0.0);
            GL.Vertex3(0.0f, 0.0f, 0.0f);

            GL.Color4(0.0f, 1.0f, 0.5f, 1.0f);
            GL.TexCoord2(0.0, 1.0);
            GL.Vertex3(0.0f, 1.0f, 0.0f);

            GL.Color4(1.0f, 0.0f, 0.5f, 1.0f);
            GL.TexCoord2(1.0, 0.0);
            GL.Vertex3(1.0f, 0.0f, 0.0f);

            GL.Color4(1.0f, 1.0f, 0.5f, 1.0f);
            GL.TexCoord2(1.0, 1.0);
            GL.Vertex3(1.0f, 1.0f, 0.0f);

            GL.End();

            GL.PopMatrix();

            SwapBuffers();
            //base.OnRenderFrame(e);
        }

    }
}