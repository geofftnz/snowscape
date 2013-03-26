using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace OpenTKExtensions
{
    public interface ISamplerParameter
    {
        SamplerParameter ParameterName { get; set; }
        void Apply(uint SamplerID);
    }
}
