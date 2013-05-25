using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Snowscape.TerrainRenderer.HDR
{
    public interface IToneMapper
    {
        Vector3 Tonemap(Vector3 col);
    }
}
