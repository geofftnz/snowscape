using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Snowscape.TerrainRenderer.Renderers.LOD
{
    public class PatchGenerator : IPatchGenerator
    {
        public int MinPatchTileSize { get; set; }

        public PatchGenerator()
        {
            MinPatchTileSize = 4;
        }


        private class QNode
        {
            public static int MinPatchTileSize = 64;

            public int depth;
            public float x1, x2, y1, y2, xc, yc;
            public int[] lod = { 0, 0, 0, 0 };
            public int tileSize;

            public QNode()
            {
            }
            public QNode(float x, float z, float x2, float z2, int tileSize, int depth)
                : this()
            {
                this.x1 = x;
                this.y1 = z;
                this.x2 = x2;
                this.y2 = z2;
                this.xc = (this.x1 + this.x2) * 0.5f;
                this.yc = (this.y1 + this.y2) * 0.5f;
                this.tileSize = tileSize;
                this.depth = depth;
            }
            public QNode(TerrainTile tile)
                : this(0f, 0f, (float)tile.Width, (float)tile.Height, tile.Width, 0)
            {
            }

            /*
            public static int GetMeshSize(int patchSize, float distance)
            {
                int meshSize = (int)(((float)patchSize * 16.0f) / distance);
            }*/

            public IEnumerable<PatchDescriptor> GetPatches(Vector3 eye)
            {

                bool emit = false;

                // node has hit the lower limit - emit
                if (this.tileSize <= MinPatchTileSize) emit = true;

                                
                if (emit)
                {
                    yield return new PatchDescriptor
                    {
                        TileSize = this.tileSize,
                        MeshSize = this.tileSize,
                        Offset = new Vector2(x1 / (float)tileSize, y1 / (float)tileSize)
                    };
                }
                else
                {
                    // 0 1
                    // 2 3
                    QNode[] child = new QNode[4];
                    child[0] = new QNode(x1, y1, xc, yc, tileSize / 2, depth + 1);
                    child[1] = new QNode(xc, y1, x2, yc, tileSize / 2, depth + 1);
                    child[2] = new QNode(x1, yc, xc, y2, tileSize / 2, depth + 1);
                    child[3] = new QNode(xc, yc, x2, y2, tileSize / 2, depth + 1);

                    foreach (var p in child.SelectMany(c => c.GetPatches(eye)))
                    {
                        yield return p;
                    }
                }

            }
        }

        public IEnumerable<PatchDescriptor> GetPatches(TerrainTile tile, Matrix4 projection, Matrix4 view, Vector3 eyeWorld)
        {
            // create root node
            var root = new QNode(tile);

            // calculate viewer position in tile coordinates
            Vector3 eyeTile = Vector3.Transform(eyeWorld, tile.InverseModelMatrix);


            return root.GetPatches(eyeTile);
        }
    }
}
