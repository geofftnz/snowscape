using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utils;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainRenderer.Atmosphere
{
    /// <summary>
    /// SkyCubeRenderer - renders the sky cube
    /// 
    /// Knows about:
    /// - nothing
    /// 
    /// Knows how to:
    /// - render sky cube
    /// 
    /// </summary>
    public class SkyCubeRenderer : GameComponentBase
    {
        private VBO vertexVBO = new VBO("skycube-vertex");
        private VBO indexVBO = new VBO("skycube-index", BufferTarget.ElementArrayBuffer);
        private ShaderProgram program = new ShaderProgram("skycube-prog");

        public SkyCubeRenderer()
            : base()
        {
            this.Loading += SkyCubeRenderer_Loading;
        }

        void SkyCubeRenderer_Loading(object sender, EventArgs e)
        {
            InitQuad();
            InitShader();
        }

        public void Render(Matrix4 projection, Matrix4 view, Vector3 eyePos, Texture skyCube)
        {
            Matrix4 invProjectionView = Matrix4.Invert(Matrix4.Mult(view, projection));

            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);

            skyCube.Bind(TextureUnit.Texture0);

            this.program
                .UseProgram()
                .SetUniform("inverse_projectionview_matrix", invProjectionView)
                .SetUniform("eyePos", eyePos)
                .SetUniform("skyCube",0);
            this.vertexVBO.Bind(this.program.VariableLocation("vertex"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);
        }

        private void InitShader()
        {
            // setup shader
            this.program.Init(
                @"SkyCube.vert",
                @"SkyCube.frag",
                new List<Variable> 
                { 
                    new Variable(0, "vertex")
                },
                new string[]
                {
                    "out_Colour",
                    "out_Normal"
                });

        }

        private void InitQuad()
        {
            Vector3[] vertex = {
                                   new Vector3(-1f,1f,0f),
                                   new Vector3(-1f,-1f,0f),
                                   new Vector3(1f,1f,0f),
                                   new Vector3(1f,-1f,0f)
                               };
            uint[] index = {
                               0,1,2,
                               1,3,2
                           };

            this.vertexVBO.SetData(vertex);
            this.indexVBO.SetData(index);
        }

    }
}
