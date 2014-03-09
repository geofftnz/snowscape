using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Snowscape.TerrainRenderer.Renderers
{
    internal static class RendererHelper
    {

        public static Vector4 GetBoxParam(this TerrainTile tile)
        {
            return new Vector4((float)tile.Width, (float)tile.Height, 0.0f, 1.0f);
        }

    }
}
