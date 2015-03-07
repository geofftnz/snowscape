using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions.Framework;

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
    public class GBufferSimpleStep : IReloadable
    {
        private GBufferShaderStep gb;
        private string inputTextureName;
        private string outputTextureName;
        private Texture outputTexture;
        private string vertexSource;
        private string fragmentSource;


        public GBufferSimpleStep(
            string name,
            string vertexSource,
            string fragmentSource,
            string inputTextureName,
            string outputTextureName,
            Texture outputTexture)
        {
            this.inputTextureName = inputTextureName;
            this.outputTextureName = outputTextureName;
            this.outputTexture = outputTexture;
            this.vertexSource = vertexSource;
            this.fragmentSource = fragmentSource;

            gb = new GBufferShaderStep(name);

        }
        public GBufferSimpleStep(
            string name,
            string fragmentSource,
            string inputTextureName,
            string outputTextureName,
            Texture outputTexture)
            : this(name, @"BasicQuad.vert", fragmentSource, inputTextureName, outputTextureName, outputTexture)
        {
        }


        public void Init()
        {
            gb.SetOutputTexture(0, outputTextureName, outputTexture);
            gb.Init(vertexSource, fragmentSource);
        }

        public void Reload()
        {
            gb.ReloadShader();   
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
