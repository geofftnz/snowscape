using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace OpenTKExtensions
{
    /// <summary>
    /// GBufferShaderStep
    /// 
    /// Applies a shader, outputs to a GBuffer
    /// </summary>
    public class GBufferShaderStep
    {
        public class TextureSlot
        {
            public string Name { get; set; }
            public Texture Texture { get; set; }
            public TextureSlot()
            {
            }
            public TextureSlot(string name, Texture tex)
            {
                this.Name = name;
                this.Texture = tex;
            }
        }

        public string Name { get; private set; }

        // Needs:
        // Quad vertex VBO
        private VBO vertexVBO;
        // Quad index VBO
        private VBO indexVBO;
        // GBuffer to encapsulate our output texture.
        private GBuffer gbuffer;
        // Shader
        private ShaderProgram program;

        // texture slots
        const int MAXSLOTS = 16;
        private TextureSlot[] textureSlot = new TextureSlot[MAXSLOTS];


        public GBufferShaderStep()
            : this("gbstep")
        {

        }
        public GBufferShaderStep(string name)
        {
            this.Name = name;
            this.vertexVBO = new VBO(this.Name + "_v");
            this.indexVBO = new VBO(this.Name + "_i", BufferTarget.ElementArrayBuffer);
            this.gbuffer = new GBuffer(this.Name + "_gb", false);
            this.program = new ShaderProgram(this.Name + "_sp");
        }

        public void SetOutputTexture(int slot, string name, Texture tex)
        {
            if (slot < 0 || slot >= MAXSLOTS)
            {
                throw new ArgumentOutOfRangeException("slot");
            }
            this.textureSlot[slot] = new TextureSlot(name, tex);
        }

        public void Init(string vertexShaderSource, string fragmentShaderSource)
        {
            this.InitVBOs();
            this.InitGBuffer();
            this.InitShader(vertexShaderSource, fragmentShaderSource);

        }

        public void Render(Action textureBinds, Action<ShaderProgram> setUniforms)
        {
            // start gbuffer
            this.gbuffer.BindForWriting();

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);

            if (textureBinds != null)
            {
                textureBinds();
            }

            this.program.UseProgram();

            if (setUniforms != null)
            {
                setUniforms(this.program);
            }

            this.vertexVBO.Bind(this.program.VariableLocation("vertex"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);

            this.gbuffer.UnbindFromWriting();
        }



        private void InitShader(string vertexShaderSource, string fragmentShaderSource)
        {
            // setup shader
            this.program.Init(
                vertexShaderSource,
                fragmentShaderSource,
                new List<Variable> 
                { 
                    new Variable(0, "vertex")
                },
                this.textureSlot.Where(ts => ts != null).Select(ts => ts.Name).ToArray()
                );
        }

        private void InitGBuffer()
        {
            if (!this.textureSlot.Any(ts => ts != null && ts.Texture != null))
            {
                throw new InvalidOperationException("No texture slots filled");
            }

            // find first texture slot, set width and height
            int width = this.textureSlot.Where(ts => ts != null && ts.Texture != null).FirstOrDefault().Texture.Width;
            int height = this.textureSlot.Where(ts => ts != null && ts.Texture != null).FirstOrDefault().Texture.Height;

            //gbuffer.SetSlot(0, outputTexture);
            for (int slot = 0; slot < MAXSLOTS; slot++)
            {
                if (this.textureSlot[slot] != null)
                {
                    if (this.textureSlot[slot].Texture != null)
                    {
                        gbuffer.SetSlot(slot, this.textureSlot[slot].Texture);
                    }
                }

            }

            gbuffer.Init(width, height);
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


    }
}
