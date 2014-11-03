using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Snowscape.TerrainRenderer.Renderers.LOD
{
    public class PatchGenerator : IPatchGenerator
    {
        public PatchGenerator()
        {
        }

        public IEnumerable<PatchDescriptor> GetPatches(TerrainTile tile, Frustum f, Vector3 eyeWorld)
        {

            // create root node
            var root = new QuadTreeNode(tile);

            return root.GetPatches(eyeWorld,f);
        }
    }
}
