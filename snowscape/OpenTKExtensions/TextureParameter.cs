using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace OpenTKExtensions
{
    public class TextureParameterInt : ITextureParameter 
    {
        public TextureParameterName ParameterName { get; set; }
        public int Value { get; set; }

        public TextureParameterInt(TextureParameterName name, int value)
        {
            this.ParameterName = name;
            this.Value = value;
        }

        public void Apply(TextureTarget target)
        {
            GL.TexParameter(target, this.ParameterName, this.Value);
        }
    }

    public class TextureParameterFloat : ITextureParameter
    {
        public TextureParameterName ParameterName { get; set; }
        public float Value { get; set; }

        public TextureParameterFloat(TextureParameterName name, float value)
        {
            this.ParameterName = name;
            this.Value = value;
        }

        public void Apply(TextureTarget target)
        {
            GL.TexParameter(target, this.ParameterName, this.Value);
        }
    }
}
