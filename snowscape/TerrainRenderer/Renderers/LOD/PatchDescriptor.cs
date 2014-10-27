using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Snowscape.TerrainRenderer.Renderers.LOD
{
    public class PatchDescriptor
    {
        // Reference to the tile we're rendering
        public TerrainTile tile { get; set; }

        // size of this patch in tile coordinates
        public int TileSize { get; set; }

        // size of this patch in mesh coordinates
        public int MeshSize { get; set; }

        // scale of this patch (difference between mesh and tile coordinate systems)
        public float Scale { get { return TileSize / MeshSize; } }

        // offset of this patch in tile coordinates
        public Vector2 Offset { get; set; }


        public PatchDescriptor()
        {
                
        }
    }
}
