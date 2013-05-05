using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL;
using Utils;

namespace Snowscape.TerrainRenderer
{
    /// <summary>
    /// TerrainLightingGenerator
    /// 
    /// Knows how to:
    /// - given a heightmap texture and min/max heights (ie from TerrainGlobal), 
    /// - generate the shadow-height map into the R channel of the shademap
    /// - generate the sky-vis (AO) map into the G channel of the shademap
    /// 
    /// </summary>
    public class TerrainLightingGenerator
    {
        // Needs:
        // Quad vertex VBO
        private VBO vertexVBO = new VBO("vertex");
        // Quad index VBO
        private VBO indexVBO = new VBO("index", BufferTarget.ElementArrayBuffer);

        // GBuffer to encapsulate our output texture.
        private GBuffer gbuffer = new GBuffer("gb", false);

        // shader
        private ShaderProgram program = new ShaderProgram("lighting");

        public int Width { get; set; }
        public int Height { get; set; }

        public TerrainLightingGenerator(int width, int height)
        {
            this.Width = width;
            this.Height = height;
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
                @"../../../Resources/Shaders/ShadowAO.vert".Load(),
                @"../../../Resources/Shaders/ShadowAO.frag".Load(),
                new List<Variable> 
                { 
                    new Variable(0, "vertex")
                },
                new string[]
                {
                    "out_ShadowAO"
                });            
        }

        private void InitGBuffer(Texture outputTexture)
        {
            gbuffer.SetSlot(0, outputTexture);
            gbuffer.Init(this.Width, this.Height);
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

        public void Render(Vector3 sunVector, Texture heightTexture, float minHeight, float maxHeight)
        {

            // start gbuffer
            this.gbuffer.BindForWriting();
            
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);

            heightTexture.Bind(TextureUnit.Texture0);

            this.program
                .UseProgram()
                .SetUniform("heightTexture", 0)
                .SetUniform("sunDirection", sunVector)
                .SetUniform("maxHeight", maxHeight);
            this.vertexVBO.Bind(this.program.VariableLocation("vertex"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);

            this.gbuffer.UnbindFromWriting();

        }

    }
}
