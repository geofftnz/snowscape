using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snowscape.TerrainRenderer.Atmosphere
{
    public class SkyRenderParams
    {
        public Vector3 eye { get; set; }
        public Vector3 sunVector { get; set; }
        public float groundLevel { get; set; }
        public float rayleighPhase { get; set; }
        public float rayleighBrightness { get; set; }
        public float miePhase { get; set; }
        public float mieBrightness { get; set; }
        public float scatterAbsorb { get; set; }
        public Vector3 Kr { get; set; }
        public Vector3 sunLight { get; set; }
        public float skyPrecalcBoundary { get; set; }

    }
}
