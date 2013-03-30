using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snowscape.TerrainRenderer
{
    /// <summary>
    /// TerrainLightingGenerator
    /// 
    /// Knows how to:
    /// - given a heightmap texture and min/max heights (ie from TerrainGlobal), 
    /// - generate the shadow-height map into the R channel of the shademap
    /// - generate the sky-vis (AO) map into the G channel of the shademap
    /// 
    /// </summary>
    public class TerrainLightingGenerator
    {
        // Needs:
        // Quad vertex VBO
        // Quad index VBO
        // GBuffer to encapsulate our output texture.


    }
}
