using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace OpenTKExtensions.Generators
{
    /// <summary>
    /// Generates streams of vertices and indexes that can be loaded into a VBO
    /// width and height are cell count
    /// </summary>
    public class GridMesh
    {
        private int width;
        private int height;
        private int vwidth { get { return width + 1; } }
        private int vheight { get { return height + 1; } }

        public GridMesh(int width, int height)
        {
            if (width < 1 || height < 1)
                throw new InvalidOperationException("Width & height must be at least 2");

            this.width = width;
            this.height = height;
        }

        public IEnumerable<Vector3> Vertices(Action<Vector3, float> axis1, Action<Vector3, float> axis2)
        {
            for (int y = 0; y < vheight; y++)
            {
                float yc = (float)y / (float)(height);

                for (int x = 0; x < vwidth; x++)
                {
                    float xc = (float)x / (float)width;

                    Vector3 v = Vector3.Zero;

                    axis1(v, xc);
                    axis2(v, yc);

                    yield return v;
                }

            }
        }

        public IEnumerable<Vector3> VerticesXZ()
        {
            return Vertices((v, a) => { v.X = a; }, (v, b) => { v.Z = b; });
        }

        public IEnumerable<uint> Indices()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 0 1
                    // 2 3

                    // 0 2 1 
                    // 1 2 3 
                    yield return (uint)(x + y * vwidth); // 0 
                    yield return (uint)(x + (y + 1) * vwidth); // 2
                    yield return (uint)((x + 1) + y * vwidth); // 1
                    yield return (uint)((x + 1) + y * vwidth); // 1
                    yield return (uint)(x + (y + 1) * vwidth); // 2
                    yield return (uint)((x + 1) + (y + 1) * vwidth); // 3

                }
            }
        }

    }
}
