﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using NLog;

namespace OpenTKExtensions
{
    /// <summary>
    /// GBufferShaderStep
    /// 
    /// Applies a shader, outputs to a GBuffer
    /// </summary>
    public class GBufferShaderStep
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public class TextureSlot
        {
            public string Name { get; set; }
            public Texture Texture { get; set; }
            public TextureTarget Target { get; set; }
            public TextureSlot()
            {
            }
            public TextureSlot(string name, Texture tex, TextureTarget target)
            {
                this.Name = name;
                this.Texture = tex;
                this.Target = target;
            }
            public TextureSlot(string name, Texture tex)
                : this(name, tex, TextureTarget.Texture2D)
            {

            }
        }

        public string Name { get; private set; }

        // Needs:
        // Quad vertex VBO
        protected VBO vertexVBO;
        // Quad index VBO
        protected VBO indexVBO;
        // GBuffer to encapsulate our output texture.
        protected GBuffer gbuffer;
        // Shader
        private string vsSource = "";
        private string fsSource = "";
        protected ShaderProgram program;

        // texture slots
        protected const int MAXSLOTS = 16;
        protected TextureSlot[] textureSlot = new TextureSlot[MAXSLOTS];



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

        public void SetOutputTexture(int slot, string name, Texture tex, TextureTarget target)
        {
            if (slot < 0 || slot >= MAXSLOTS)
            {
                throw new ArgumentOutOfRangeException("slot");
            }
            this.textureSlot[slot] = new TextureSlot(name, tex, target);
        }

        public void SetOutputTexture(int slot, string name, Texture tex)
        {
            SetOutputTexture(slot, name, tex, TextureTarget.Texture2D);
        }

        public void ClearOutputTexture(int slot)
        {
            if (slot < 0 || slot >= MAXSLOTS)
            {
                throw new ArgumentOutOfRangeException("slot");
            }
            this.textureSlot[slot] = null;
        }

        public void Init(string vertexShaderSource, string fragmentShaderSource)
        {
            this.InitVBOs();
            this.InitGBuffer();
            this.InitShader(vertexShaderSource, fragmentShaderSource);

        }

        public virtual void Render(Action textureBinds, Action<ShaderProgram> setUniforms)
        {
            this.Render(textureBinds, setUniforms, () =>
            {
                this.vertexVBO.Bind(this.program.VariableLocation("vertex"));
                this.indexVBO.Bind();
                GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);
            });
        }

        public virtual void Render(Action textureBinds, Action<ShaderProgram> setUniforms, Action renderAction)
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

            if (renderAction != null)
            {
                renderAction();
            }

            this.gbuffer.UnbindFromWriting();
        }


        private void InitShader(string vertexShaderSource, string fragmentShaderSource)
        {
            vsSource = vertexShaderSource;
            fsSource = fragmentShaderSource;

            if (!ReloadShader())
                throw new InvalidOperationException("Could not load shader");

        }

        private ShaderProgram LoadShaderProgram()
        {
            var program = new ShaderProgram(Name);

            // setup shader
            program.Init(
                vsSource,
                fsSource,
                new List<Variable> 
                { 
                    new Variable(0, "vertex")
                },
                this.textureSlot.Where(ts => ts != null).Select(ts => ts.Name).ToArray()
                );

            return program;
        }

        public bool ReloadShader()
        {
            try
            {
                ShaderProgram p = LoadShaderProgram();

                if (p == null)
                {
                    throw new InvalidOperationException("ReloadShader() returned null, but didn't throw an exception");
                }

                if (this.program != null)
                {
                    this.program.Unload();
                }
                this.program = p;
                return true;
            }
            catch (Exception ex)
            {
                if (log != null)
                {
                    log.Warn("Could not reload shader program {0}: {1}", Name, ex.GetType().Name + ": " + ex.Message);
                }
            }
            return false;

        }

        protected virtual void InitGBuffer()
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
                                    new Vector3(-1f,-1f,0f),
                                    new Vector3(3f,-1f,0f),
                                    new Vector3(-1f,3f,0f)
                                };
            uint[] index = {
                                0,1,2
                            };

            this.vertexVBO.SetData(vertex);
            this.indexVBO.SetData(index);
        }

        private void InitVBOsq()
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

        public void ClearColourBuffer(int drawBuffer, Vector4 colour)
        {
            this.gbuffer.ClearColourBuffer(drawBuffer, colour);
        }

        public int ShaderVariableLocation(string name)
        {
            return this.program.VariableLocation(name);
        }

    }
}
