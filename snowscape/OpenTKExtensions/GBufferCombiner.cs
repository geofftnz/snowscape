using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace OpenTKExtensions
{
    /// <summary>
    /// GBuffer Combiner
    /// 
    /// This class knows how to take a GBuffer and render it to screen (or another buffer)
    /// </summary>
    public class GBufferCombiner
    {
        public GBuffer GBuffer { get; set; }

        public ShaderProgram CombineProgram { get; set; }

        private VBO gbufferCombineVertexVBO = new VBO("gbvertex");
        private VBO gbufferCombineTexcoordVBO = new VBO("gbtexcoord");
        private VBO gbufferCombineIndexVBO = new VBO("gbindex", BufferTarget.ElementArrayBuffer);

        public bool IsInit { get; private set; }

        public GBufferCombiner()
        {
            this.IsInit = false;
        }

        public GBufferCombiner(GBuffer gb)
            : this()
        {
            this.GBuffer = gb;
        }

        public GBufferCombiner(GBuffer gb, ShaderProgram prog)
            : this(gb)
        {
            this.CombineProgram = prog;
        }

        public void Init()
        {
            if (this.IsInit) return;

            var vertex = new Vector3[4];
            var texcoord = new Vector2[4];
            uint[] index = { 0, 1, 3, 1, 2, 3 };

            int i = 0;

            vertex[i] = new Vector3(0.0f, 0.0f, 0.0f);
            texcoord[i] = new Vector2(0.0f, 0.0f);
            i++;
            vertex[i] = new Vector3(1.0f, 0.0f, 0.0f);
            texcoord[i] = new Vector2(1.0f, 0.0f);
            i++;
            vertex[i] = new Vector3(1.0f, 1.0f, 0.0f);
            texcoord[i] = new Vector2(1.0f, 1.0f);
            i++;
            vertex[i] = new Vector3(0.0f, 1.0f, 0.0f);
            texcoord[i] = new Vector2(0.0f, 1.0f);
            i++;

            this.gbufferCombineVertexVBO.SetData(vertex);
            this.gbufferCombineTexcoordVBO.SetData(texcoord);
            this.gbufferCombineIndexVBO.SetData(index);

            this.IsInit = true;
        }

        public void Render(Matrix4 projection, Matrix4 view, Action<ShaderProgram> setUniforms)
        {
            if (this.GBuffer == null || this.CombineProgram == null)
            {
                return;
            }

            this.Init();
            this.BindTextures();
            this.CombineProgram
                .UseProgram()
                .SetUniform("projection_matrix", projection)
                .SetUniform("modelview_matrix", view);

            setUniforms(this.CombineProgram);

            this.gbufferCombineVertexVBO.Bind(this.CombineProgram.VariableLocation("vertex"));
            this.gbufferCombineTexcoordVBO.Bind(this.CombineProgram.VariableLocation("in_texcoord0"));
            this.gbufferCombineIndexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.gbufferCombineIndexVBO.Length, DrawElementsType.UnsignedInt, 0);

        }

        private void BindTextures()
        {
            for (int i = 0; i < GBuffer.MAXSLOTS; i++)
            {
                var tex = this.GBuffer.GetTextureAtSlotOrNull(i);
                if (tex != null)
                {
                    tex.Bind(TextureUnit.Texture0 + i);
                }
            }
        }

    }
}
