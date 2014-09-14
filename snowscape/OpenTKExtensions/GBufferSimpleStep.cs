using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace OpenTKExtensions
{
    /// <summary>
    /// Simple shader step
    /// 
    /// Input: 1 texture
    /// Output: 1 texture
    /// Vertex: BasicQuad
    /// Fragment: supplied
    /// </summary>
    public class GBufferSimpleStep
    {
        private GBufferShaderStep gb;
        private string inputTextureName;
        private string outputTextureName;
        private Texture outputTexture;
        private string fragmentSource;
        

        public GBufferSimpleStep(
            string name, 
            string fragmentSource, 
            string inputTextureName,
            string outputTextureName, 
            Texture outputTexture)
        {
            this.inputTextureName = inputTextureName;
            this.outputTextureName = outputTextureName;
            this.outputTexture = outputTexture;
            this.fragmentSource = fragmentSource;

            gb = new GBufferShaderStep(name);

        }

        public void Init()
        {
            gb.SetOutputTexture(0, outputTextureName, outputTexture);
            gb.Init(@"BasicQuad.vert", fragmentSource);
        }

        public void Render(Texture inputTexture, Action<ShaderProgram> SetUniforms = null)
        {
            gb.Render(() =>
            {
                inputTexture.Bind(TextureUnit.Texture0);
            },
            (sp) =>
            {
                sp.SetUniform(inputTextureName, 0);
                if (SetUniforms != null)
                {
                    SetUniforms(sp);
                }
            });
        }

    }
}
