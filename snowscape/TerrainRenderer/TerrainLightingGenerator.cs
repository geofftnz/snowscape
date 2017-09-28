using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL4;
using Utils;
using OpenTKExtensions.Framework;

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
    public class TerrainLightingGenerator : GameComponentBase, IRenderable
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

        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool Visible { get; set; }
        public int DrawOrder { get; set; }

        // parameters
        public Texture OutputTexture { get; set; }
        public Vector3 SunVector { get; set; }
        public Texture HeightTexture { get; set; }
        public float MaxTerrainHeight { get; set; }


        public TerrainLightingGenerator(int width, int height)
            : base()
        {
            this.Width = width;
            this.Height = height;

            this.Visible = true;
            this.DrawOrder = 0;

            this.Loading += TerrainLightingGenerator_Loading;
        }


        public TerrainLightingGenerator(int width, int height, Texture outputTexture)
            : this(width, height)
        {
            this.OutputTexture = outputTexture;
        }


        void TerrainLightingGenerator_Loading(object sender, EventArgs e)
        {
            if (this.OutputTexture == null)
            {
                throw new InvalidOperationException("OutputTexture not set");
            }

            base.Load();

            // init VBOs
            this.InitVBOs();

            // init GBuffer
            this.InitGBuffer(this.OutputTexture);

            // init Shader
            this.InitShader();
        }


        private void InitShader()
        {
            // setup shader
            this.program.Init(
                @"ShadowAO.vert",
                @"ShadowAO.frag",
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

        public void Render(IFrameRenderData frameData)
        {
            if (this.HeightTexture == null)
            {
                throw new InvalidOperationException("HeightTexture not set");
            }


            // start gbuffer
            this.gbuffer.BindForWriting();

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);

            this.HeightTexture.Bind(TextureUnit.Texture0);

            this.program
                .UseProgram()
                .SetUniform("heightTexture", 0)
                .SetUniform("sunDirection", this.SunVector)
                .SetUniform("maxHeight", this.MaxTerrainHeight);
            this.vertexVBO.Bind(this.program.VariableLocation("vertex"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);

            this.gbuffer.UnbindFromWriting();

        }

    }
}
