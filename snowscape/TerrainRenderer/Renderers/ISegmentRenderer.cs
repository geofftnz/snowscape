using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snowscape.TerrainRenderer.Renderers
{
    public interface ISegmentRenderer
    {
        new void Render(
            Snowscape.TerrainRenderer.TerrainTile tile, 
            Snowscape.TerrainRenderer.TerrainGlobal terrainGlobal, 
            OpenTK.Matrix4 projection, 
            OpenTK.Matrix4 view, 
            OpenTK.Vector3 eyePos,
            float angleOffset,
            float angleExtent,
            float radiusOffset,
            float radiusExtent
            );
    }
}
