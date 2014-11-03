using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Snowscape.TerrainRenderer.Renderers.LOD
{
    /// <summary>
    /// Defines a class that can take a terrain tile and produce a number of patches for rendering
    /// </summary>
    public interface IPatchGenerator
    {
        IEnumerable<PatchDescriptor> GetPatches(TerrainTile tile, Frustum f, Vector3 eye);
    }
}
