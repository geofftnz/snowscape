using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Generators
{
    /// <summary>
    /// Returns vertices and indices for a screen-covering triangle, for the 0-1 screen
    /// </summary>
    public static class ScreenTri
    {
        public static IEnumerable<Vector3> Vertices()
        {
            yield return new Vector3(0.0f, 0.0f, 0.0f);
            yield return new Vector3(2.0f, 0.0f, 0.0f);
            yield return new Vector3(0.0f, 2.0f, 0.0f);
        }
        public static IEnumerable<uint> Indices()
        {
            yield return 0;
            yield return 1;
            yield return 2;
        }
    }
}
