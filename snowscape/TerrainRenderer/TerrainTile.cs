using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Snowscape.TerrainRenderer
{

    /// <summary>
    /// A Terrain Tile...
    /// 
    /// knows about:
    /// - its bounding box (VBO)
    /// - its heightmap (texture)
    /// - its normalmap (texture)
    /// - its shading data (textures)
    /// 
    /// knows how to:
    /// - generate its bounding box from the supplied (full-scale) heightmap
    /// - generate its textures from a subset of the supplied full-scale textures
    /// </summary>
    public class TerrainTile
    {
        public int Width { get; set; }
        public int Height { get; set; }

    }
}
