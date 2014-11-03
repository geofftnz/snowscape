using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions.Framework;

namespace OpenTKExtensions.Components
{
    public class LineBuffer : GameComponentBase
    {
        private readonly int maxlines;

        private Vector3[] vertex;
        private Vector4[] colour;
        private uint[] index;
        private int numLines=0;
        private bool needRefresh = false;

        private VBO vertexVBO = new VBO("linebuffer-v");
        private VBO colourVBO = new VBO("linebuffer-c");
        private VBO indexVBO = new VBO("linebuffer-i", BufferTarget.ElementArrayBuffer);

        private ShaderProgram shader = new ShaderProgram("linebuffer");

        private Vector3 current = Vector3.Zero;
        private Vector4 currentColour = new Vector4(1f);

        public LineBuffer(int maxlines)
        {
            this.maxlines = maxlines;

            this.vertex = new Vector3[this.maxlines * 2];
            this.colour = new Vector4[this.maxlines * 2];
            this.index = new uint[this.maxlines * 2];

            this.Loading += LineBuffer_Loading;
            this.Unloading += LineBuffer_Unloading;
        }


        public LineBuffer()
            : this(4096)
        {

        }


        private void LineBuffer_Loading(object sender, EventArgs e)
        {
            for (int i = 0; i < this.maxlines * 2; i++)
            {
                vertex[i] = Vector3.Zero;
                colour[i] = Vector4.Zero;
                index[i] = (uint)i;
            }

            vertexVBO.Init();
            vertexVBO.SetData(vertex);

            colourVBO.Init();
            colourVBO.SetData(colour);

            indexVBO.Init();
            indexVBO.SetData(index);

            // setup shader
            this.shader.Init(
                @"LineBuffer.glsl|VS",
                @"LineBuffer.glsl|FS",
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "colour") 
                });

        }

        private void LineBuffer_Unloading(object sender, EventArgs e)
        {
            shader.Unload();
        }

        public void ClearLines()
        {
            this.numLines = 0;
        }

        public void AddLine(Vector3 p0, Vector3 p1, Vector4 col)
        {
            if (this.numLines < this.maxlines)
            {
                int i = this.numLines * 2;

                vertex[i] = p0;
                colour[i] = col;

                vertex[i+1] = p1;
                colour[i+1] = col;

                this.numLines++;
                needRefresh = true;
            }
        }

        public void MoveTo(Vector3 p0)
        {
            current = p0;
        }

        public void LineTo(Vector3 p0, Vector4 col)
        {
            AddLine(current, p0, col);
            current = p0;
        }

        public void LineTo(Vector3 p0)
        {
            LineTo(p0, currentColour);
        }

        public void SetColour(Vector4 col)
        {
            currentColour = col;
        }

        public void RefreshBuffers()
        {
            if (needRefresh)
            {
                vertexVBO.SetData(vertex);
                colourVBO.SetData(colour);

                needRefresh = false;
            }
        }

        public void Render(Matrix4 model, Matrix4 view, Matrix4 projection)
        {
            RefreshBuffers();

            //GL.Disable(EnableCap.Texture2D);
            //GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            this.shader
                .UseProgram()
                .SetUniform("projection_matrix", projection)
                .SetUniform("model_matrix", model)
                .SetUniform("view_matrix", view);

            this.vertexVBO.Bind(this.shader.VariableLocation("vertex"));
            this.colourVBO.Bind(this.shader.VariableLocation("colour"));
            this.indexVBO.Bind();

            GL.DrawElements(BeginMode.Lines, this.numLines * 2, DrawElementsType.UnsignedInt, 0);
        }



    }
}
