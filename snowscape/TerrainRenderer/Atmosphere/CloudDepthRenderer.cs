using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utils;

namespace Snowscape.TerrainRenderer.Atmosphere
{
    /// <summary>
    /// CloudDepthRenderer - pre-calculates the optical depth of the cloud layer with respect to the light source.
    /// 
    /// Knows about:
    /// - nothing
    /// 
    /// Knows how to:
    /// - calculate the min, max and relative density of the cloud layer.
    /// 
    /// Needs:
    /// - sun direction
    /// - xz scale factor: cloudScale.xz
    /// - cloud layer thickness: cloudScale.y
    ///  (need to convert the 0-1 of texel-space into world space)
    /// 
    /// </summary>
    public class CloudDepthRenderer
    {
         // Needs:
        // Quad vertex VBO
        private VBO vertexVBO = new VBO("cloud-depth-vertex");
        // Quad index VBO
        private VBO indexVBO = new VBO("cloud-depth-index", BufferTarget.ElementArrayBuffer);

        // GBuffer to encapsulate our output texture.
        private GBuffer gbuffer = new GBuffer("cloud-depth-gbuffer", false);

        // shader
        private ShaderProgram program = new ShaderProgram("cloud-depth");

        public CloudDepthRenderer()
        {
        }

        public void Init(Texture outputTexture)
        {
            // init VBOs
            this.InitVBOs();

            // init GBuffer
            this.InitGBuffer(outputTexture);

            // init Shader
            this.InitShader();

        }

        private void InitShader()
        {
            // setup shader
            this.program.Init(
                @"../../../Resources/Shaders/CloudDepth.vert".Load(),
                @"../../../Resources/Shaders/CloudDepth.frag".Load(),
                new List<Variable> 
                { 
                    new Variable(0, "vertex")
                },
                new string[]
                {
                    "out_CloudDepth"
                });            
        }

        private void InitGBuffer(Texture outputTexture)
        {
            gbuffer.SetSlot(0, outputTexture);
            gbuffer.Init(outputTexture.Width, outputTexture.Height);
        }

        private void InitVBOs()
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

        public void Render(Texture cloudTexture, Vector3 sunVector, Vector3 cloudScale)
        {

            // start gbuffer
            this.gbuffer.BindForWriting();
            
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);

            cloudTexture.Bind(TextureUnit.Texture0);

            this.program
                .UseProgram()
                .SetUniform("cloudTexture", 0)
                .SetUniform("sunDirection", sunVector)
                .SetUniform("cloudScale", cloudScale);
            this.vertexVBO.Bind(this.program.VariableLocation("vertex"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);

            this.gbuffer.UnbindFromWriting();

        }
    }
}
