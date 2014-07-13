using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace OpenTKExtensions.UI
{

    /// <summary>
    /// Renders frame time breakdown as a stacked bar graph.
    /// 
    /// Uses VBOs for vertex and colour
    /// 
    /// Render element: triangles.
    /// 
    /// Builds vertex/colour/index VBOs of triangles.
    /// Vertex and colour are updated from performance data each frame
    /// Index is constant
    /// 
    /// </summary>
    public class FrameTimeGraphRenderer
    {
        private const int MAX_QUADS = 16384;

        private VBO vertexVBO;
        private VBO colourVBO;
        private VBO indexVBO;
        private ShaderProgram shaderProgram;

        private Vector3[] vertex = new Vector3[MAX_QUADS * 4];
        private Vector4[] colour = new Vector4[MAX_QUADS * 4];
        private uint[] index = new uint[MAX_QUADS * 6];
        private int numQuads = 0;
        private bool needUpload = true;

        
        #region shaders
        private const string VERTEXPROGRAM =
        @"
            #version 140
            precision highp float;
 
            uniform mat4        projection_matrix;
            uniform mat4        modelview_matrix;
            uniform float       alpha;

            in vec3 vertex;
            in vec4 colour;

            out vec4 col;
 
            void main() {
                col = colour * vec4(1.0,1.0,1.0,alpha);
                gl_Position = projection_matrix * modelview_matrix * vec4(vertex, 1.0);
            }
        ";
        private const string FRAGMENTPROGRAM =
        @"
            #version 140
            precision highp float;

            in vec4 col;

            out vec4 out_Colour;

            void main(void)
            {
                out_Colour = col;
            }
        ";
        #endregion

        public FrameTimeGraphRenderer(int numSamples, int maxSteps)
        {
            vertexVBO = new VBO("frametime-vertex");
            colourVBO = new VBO("frametime-colour");
            indexVBO = new VBO("frametime-index", OpenTK.Graphics.OpenGL.BufferTarget.ElementArrayBuffer);
            shaderProgram = new ShaderProgram("frametime-program");
        }

        public void Init()
        {
            InitVBOs();
            InitShader();
        }

        public void Clear()
        {
            this.numQuads = 0;
        }

        public void AddQuad(Vector3 origin, Vector3 ubasis, Vector3 vbasis, Vector4 col)
        {
            if (this.numQuads < MAX_QUADS)
            {
                int i = this.numQuads * 4;
                vertex[i + 0] = origin;
                vertex[i + 1] = origin + ubasis;
                vertex[i + 2] = origin + vbasis;
                vertex[i + 3] = origin + ubasis + vbasis;

                colour[i + 0] = col;
                colour[i + 1] = col;
                colour[i + 2] = col;
                colour[i + 3] = col;

                this.numQuads++;
                needUpload = true;
            }
        }

        public void Render(Matrix4 projection, Matrix4 view, float opacity)
        {
            if (this.numQuads == 0)
            {
                return;
            }

            if (this.needUpload)
            {
                this.vertexVBO.SetData(vertex);
                this.colourVBO.SetData(colour);
                this.needUpload = false;
            }

            GL.Disable(EnableCap.CullFace);
            //GL.CullFace(CullFaceMode.Back);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);


            this.shaderProgram
                .UseProgram()
                .SetUniform("projection_matrix", projection)
                .SetUniform("modelview_matrix", view)
                .SetUniform("alpha", opacity);
            this.vertexVBO.Bind(this.shaderProgram.VariableLocation("vertex"));
            this.colourVBO.Bind(this.shaderProgram.VariableLocation("colour"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.numQuads * 6, DrawElementsType.UnsignedInt, 0);

        }

        private void InitShader()
        {
            this.shaderProgram.Init(
                VERTEXPROGRAM,
                FRAGMENTPROGRAM,
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "colour") 
                },
                null,
                null);

        }


        private void InitVBOs()
        {
            for (int i = 0; i < MAX_QUADS*4; i++)
            {
                vertex[i] = Vector3.Zero;
                colour[i] = Vector4.Zero;
            }

            for (int i = 0; i < MAX_QUADS; i++)
            {
                    index[i * 6 + 0] = (uint)(i *4 + 0);
                    index[i * 6 + 1] = (uint)(i *4 + 1);
                    index[i * 6 + 2] = (uint)(i *4 + 2);
                    index[i * 6 + 3] = (uint)(i *4 + 1);
                    index[i * 6 + 4] = (uint)(i *4 + 3);
                    index[i * 6 + 5] = (uint)(i *4 + 2);
            }

            this.vertexVBO.SetData(vertex);
            this.colourVBO.SetData(colour);
            this.indexVBO.SetData(index);
        }

    }
}
