using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace OpenTKExtensions
{
    public class RenderInfo
    {
        public Matrix4 Projection { get; set; }
        public Matrix4 Modelview { get; set; }
        public Vector2 Resolution { get; set; }
    }
}
