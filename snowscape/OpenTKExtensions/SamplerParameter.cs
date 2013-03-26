using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace OpenTKExtensions
{
    public class SamplerObjectParameterFloat : ISamplerParameter 
    {
        public SamplerParameter ParameterName { get; set; }
        public float Value { get; set; }

        public SamplerObjectParameterFloat(SamplerParameter name, float value)
        {
            this.ParameterName = name;
            this.Value = value;
        }

        public void Apply(uint samplerID)
        {
            GL.SamplerParameter(samplerID, this.ParameterName, this.Value);
        }
    }

    public class SamplerObjectParameterInt : ISamplerParameter
    {
        public SamplerParameter ParameterName { get; set; }
        public int Value { get; set; }

        public SamplerObjectParameterInt(SamplerParameter name, int value)
        {
            this.ParameterName = name;
            this.Value = value;
        }

        public void Apply(uint samplerID)
        {
            GL.SamplerParameter(samplerID, this.ParameterName, this.Value);
        }
    }
}
