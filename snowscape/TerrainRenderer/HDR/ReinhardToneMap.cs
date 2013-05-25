using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Snowscape.TerrainRenderer.HDR
{
    public class ReinhardToneMap : IToneMapper
    {
        public float WhiteLevel { get; set; }

        public ReinhardToneMap()
        {
            this.WhiteLevel = 1.0f;
        }

        public Vector3 Tonemap(Vector3 col)
        {
            return Vector3.Divide(Vector3.Multiply(col, (Vector3.One + (col / (WhiteLevel * WhiteLevel)))), (Vector3.One + col));
        }
    }
}
