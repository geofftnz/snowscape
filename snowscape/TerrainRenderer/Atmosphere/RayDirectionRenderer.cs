﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Utils;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainRenderer.Atmosphere
{
    /// <summary>
    /// RayDirectionRenderer - renders the ray direction for each pixel into the gbuffer
    /// 
    /// Knows about:
    /// - nothing
    /// 
    /// Knows how to:
    /// - render ray direction based on the given projection
    /// 
    /// </summary>
    public class RayDirectionRenderer : GameComponentBase
    {
        private VBO vertexVBO = new VBO("sky-vertex");
        private VBO indexVBO = new VBO("sky-index", BufferTarget.ElementArrayBuffer);
        private ShaderProgram program = new ShaderProgram("sky-prog");

        public RayDirectionRenderer()
            : base()
        {
            this.Loading += RayDirectionRenderer_Loading;
        }

        void RayDirectionRenderer_Loading(object sender, EventArgs e)
        {
            InitQuad();
            InitShader();
        }

        public void Render(Matrix4 projection, Matrix4 view, Vector3 eyePos)
        {
            Matrix4 invProjectionView = Matrix4.Invert(Matrix4.Mult(view, projection));

            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);

            this.program
                .UseProgram()
                .SetUniform("inverse_projectionview_matrix", invProjectionView)
                .SetUniform("eyePos", eyePos);
            this.vertexVBO.Bind(this.program.VariableLocation("vertex"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);
        }

        private void InitShader()
        {
            // setup shader
            this.program.Init(
                @"SkyRayDirection.vert",
                @"SkyRayDirection.frag",
                new List<Variable> 
                { 
                    new Variable(0, "vertex"),
                    new Variable(1, "vcoord")
                },
                new string[]
                {
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
