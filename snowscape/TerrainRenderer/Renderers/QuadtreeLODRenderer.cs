using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTKExtensions;

namespace Snowscape.TerrainRenderer.Renderers
{
    /// <summary>
    /// Composite renderer that subdivides a larger tile depending on the distance to the viewer.
    /// 
    /// Tile plane is XZ, height is Y
    /// 
    /// When rendering:
    /// - if the viewer's detail radius is completely outside of our box, then use the fileTileRenderer
    /// - otherwise subdivide the tile into 4 subtiles.
    /// - for each subtile:
    ///   - if the viewer's detail radius is completely outside of the subtile, then use the subTileRenderer.
    ///   - if the viewer's detail radius is inside our sub
    ///   - otherwise split and recurse.
    /// </summary>
    public class QuadtreeLODRenderer : ITileRenderer
    {
        private ITileRenderer fullTileRenderer; // full tile in distance.
        private ITileRenderer subTileRenderer; // renders partial tile, no extra detail.
        private ITileRenderer subTileDetailRenderer; // renders partial tile with added detail.

        /// <summary>
        /// Distance at which to start generating extra detail.
        /// </summary>
        public float DetailRadius { get; set; }

        protected class QuadTreeVertex
        {
            public Vector3 Position { get; set; }

        }

        protected class QuadTreeNode
        {
            public QuadTreeVertex TopLeft { get; set; }
            public QuadTreeVertex TopRight { get; set; }
            public QuadTreeVertex BottomLeft { get; set; }
            public QuadTreeVertex BottomRight { get; set; }
            public float MinHeight { get; set; }
            public float MaxHeight { get; set; }

            public float NodeWidth
            {
                get
                {
                    return TopRight.Position.X - TopLeft.Position.X;
                }
            }
            public float NodeHeight
            {
                get
                {
                    return BottomLeft.Position.Z - TopLeft.Position.Z;
                }
            }

            public Vector3 TileCentre
            {
                get
                {
                    return new Vector3(
                            (TopRight.Position.X + TopLeft.Position.X) * 0.5f,
                            (MaxHeight + MinHeight) * 0.5f,
                            (TopLeft.Position.Z + BottomLeft.Position.Z) * 0.5f
                        );
                }
            }

            public int TileSize { get; set; }

            public QuadTreeNode()
            {

            }

            /// <summary>
            /// adapted from http://stackoverflow.com/questions/401847/circle-rectangle-collision-detection-intersection/402010#402010
            /// </summary>
            /// <param name="viewer"></param>
            /// <param name="detailRadius"></param>
            /// <returns></returns>
            public bool IsViewerInDetailRange(Vector3 viewer, float detailRadius)
            {
                Vector3 detailDistance = (viewer - this.TileCentre).Abs();

                // outside
                if (detailDistance.X > NodeWidth * 0.5f + detailRadius)
                {
                    return false;
                }
                if (detailDistance.Y > (MaxHeight - MinHeight) * 0.5f + detailRadius)
                {
                    return false;
                }
                if (detailDistance.Z > NodeHeight * 0.5f + detailRadius)
                {
                    return false;
                }

                // inside
                if (detailDistance.X <= NodeWidth * 0.5f)
                {
                    return true;
                }
                if (detailDistance.Y <= (MaxHeight - MinHeight) * 0.5f)
                {
                    return true;
                }
                if (detailDistance.Z <= NodeHeight * 0.5f)
                {
                    return true;
                }

                float cornerDistanceSquared = (detailDistance - new Vector3(NodeWidth * 0.5f, (MaxHeight - MinHeight) * 0.5f, NodeHeight * 0.5f)).LengthSquared;

                return cornerDistanceSquared <= detailRadius * detailRadius;

            }

        }

        public QuadtreeLODRenderer(ITileRenderer fullTile, ITileRenderer subTile, ITileRenderer subTileDetail)
        {
            this.fullTileRenderer = fullTile;
            this.subTileRenderer = subTile;
            this.subTileDetailRenderer = subTileDetail;
            this.DetailRadius = 32.0f;
        }

        public void Load()
        {
            // assume child renderers are loaded by caller
        }

        public void Render(TerrainTile tile, Matrix4 projection, Matrix4 view, Vector3 eye)
        {
            // construct our top-level quadtree node
            var root = new QuadTreeNode()
            {
                TopLeft = new QuadTreeVertex()
                {
                    Position = Vector3.Transform(new Vector3(0f, 0f, 0f), tile.ModelMatrix)
                },
                TopRight = new QuadTreeVertex()
                {
                    Position = Vector3.Transform(new Vector3(1f, 0f, 0f), tile.ModelMatrix)
                },
                BottomLeft = new QuadTreeVertex()
                {
                    Position = Vector3.Transform(new Vector3(0f, 0f, 1f), tile.ModelMatrix)
                },
                BottomRight = new QuadTreeVertex()
                {
                    Position = Vector3.Transform(new Vector3(1f, 0f, 1f), tile.ModelMatrix)
                },
                MinHeight = tile.MinHeight,
                MaxHeight = tile.MaxHeight
            };

            // check to see if we're completely outside the detail radius
            if (!root.IsViewerInDetailRange(eye, this.DetailRadius))
            {
                fullTileRenderer.Render(tile, projection, view, eye);
            }
            else // we need to split and recurse
            {

            }
        }

        public void Unload()
        {

        }
    }
}
