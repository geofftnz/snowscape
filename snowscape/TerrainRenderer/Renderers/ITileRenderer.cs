using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Snowscape.TerrainRenderer.Renderers
{
    /// <summary>
    /// A Tile Renderer...
    /// 
    /// Knows about:
    /// - the things it needs to render an arbitrary tile
    /// 
    /// Knows how to:
    /// - set itself up to render a tile
    /// - render a tile
    /// - clean itself up
    /// </summary>
    public interface ITileRenderer
    {
        void Load();
        void Render(TerrainTile tile, Matrix4 projection, Matrix4 view, Vector3 eye);
        void Unload();
    }
}
