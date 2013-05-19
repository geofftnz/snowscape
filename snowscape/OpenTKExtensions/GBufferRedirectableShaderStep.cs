using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace OpenTKExtensions
{
    public class GBufferRedirectableShaderStep : GBufferShaderStep
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public GBufferRedirectableShaderStep(string name, int width, int height)
            : base(name)
        {
            this.Width = width;
            this.Height = height;
        }

        public GBufferRedirectableShaderStep(int width, int height)
            : base()
        {
            this.Width = width;
            this.Height = height;
        }

        protected override void InitGBuffer()
        {
            gbuffer.Init(this.Width, this.Height);
        }

        public virtual void Render(Action textureBinds, Action<ShaderProgram> setUniforms, params GBuffer.TextureSlot[] outputTextures)
        {
            // start gbuffer
            this.gbuffer.BindForWritingTo(outputTextures);

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

    }
}
