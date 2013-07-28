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
        private ITileRenderer fullTileDistantRenderer; // full tile in distance.
        private ITileRenderer fullTileNearRenderer; // full tile nearer.
        private IPatchRenderer subTileRenderer; // renders partial tile, no extra detail.
        private IPatchRenderer subTileDetailRenderer; // renders partial tile with added detail.

        /// <summary>
        /// Distance at which to start generating extra detail.
        /// </summary>
        public float DetailRadius { get; set; }

        /// <summary>
        /// Distance at which to use the distant full-tile renderer
        /// </summary>
        public float DistantTileRadius { get; set; }

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

            public void Render(TerrainTile tile, Matrix4 projection, Matrix4 view, Vector3 eye, Vector3 reye, float detailRadius, IPatchRenderer tileRenderer, IPatchRenderer tileDetailRenderer)
            {
                // can we render this tile without subdividing?

                // we can render now once we've got to a small enough tile
                if (this.TileSize <= 4)
                {
                    tileDetailRenderer.Scale = (float)this.TileSize / (float)tile.Width;
                    tileDetailRenderer.Offset = (this.TopLeft.Position.Xz / (float)tile.Width);
                    tileDetailRenderer.Render(tile, projection, view, eye);
                    return;
                }

                if (!IsViewerInDetailRange(reye, detailRadius))
                {
                    // we can if we're outside the detail radius
                    tileRenderer.Width = this.TileSize;
                    tileRenderer.Height = this.TileSize;
                    tileRenderer.Scale = (float)this.TileSize / (float)tile.Width;
                    tileRenderer.Offset = (this.TopLeft.Position.Xz / (float)tile.Width);
                    tileRenderer.Render(tile, projection, view, eye);
                    return;
                }

                // split into 4 - need centre pos
                Vector3 centre = (TopLeft.Position + BottomRight.Position) * 0.5f;

                // top left child
                new QuadTreeNode()
                {
                    TopLeft = this.TopLeft,
                    TopRight = new QuadTreeVertex() { Position = new Vector3(centre.X, 0f, this.TopLeft.Position.Z) },
                    BottomLeft = new QuadTreeVertex() { Position = new Vector3(this.TopLeft.Position.X, 0f, centre.Z) },
                    BottomRight = new QuadTreeVertex() { Position = centre },
                    MinHeight = this.MinHeight,
                    MaxHeight = this.MaxHeight,
                    TileSize = this.TileSize / 2
                }.Render(tile, projection, view, eye, reye, detailRadius, tileRenderer, tileDetailRenderer);

                // top right child
                new QuadTreeNode()
                {
                    TopLeft = new QuadTreeVertex() { Position = new Vector3(centre.X, 0f, this.TopRight.Position.Z) },
                    TopRight = this.TopRight,
                    BottomLeft = new QuadTreeVertex() { Position = centre },
                    BottomRight = new QuadTreeVertex() { Position = new Vector3(this.TopRight.Position.X, 0f, centre.Z) },
                    MinHeight = this.MinHeight,
                    MaxHeight = this.MaxHeight,
                    TileSize = this.TileSize / 2
                }.Render(tile, projection, view, eye, reye, detailRadius, tileRenderer, tileDetailRenderer);

                // bottom left child
                new QuadTreeNode()
                {
                    TopLeft = new QuadTreeVertex() { Position = new Vector3(this.BottomLeft.Position.X, 0f, centre.Z) },
                    TopRight = new QuadTreeVertex() { Position = centre },
                    BottomLeft = this.BottomLeft,
                    BottomRight = new QuadTreeVertex() { Position = new Vector3(centre.X, 0f, this.BottomLeft.Position.Z) },
                    MinHeight = this.MinHeight,
                    MaxHeight = this.MaxHeight,
                    TileSize = this.TileSize / 2
                }.Render(tile, projection, view, eye, reye, detailRadius, tileRenderer, tileDetailRenderer);

                // bottom right child
                new QuadTreeNode()
                {
                    TopLeft = new QuadTreeVertex() { Position = centre }, 
                    TopRight = new QuadTreeVertex() { Position = new Vector3(this.BottomRight.Position.X, 0f, centre.Z) },
                    BottomLeft = new QuadTreeVertex() { Position = new Vector3(centre.X, 0f, this.BottomRight.Position.Z) },
                    BottomRight = this.BottomRight,
                    MinHeight = this.MinHeight,
                    MaxHeight = this.MaxHeight,
                    TileSize = this.TileSize / 2
                }.Render(tile, projection, view, eye, reye, detailRadius, tileRenderer, tileDetailRenderer);

            }
        }

        public QuadtreeLODRenderer(ITileRenderer fullTileDistant, ITileRenderer fullTileNear, IPatchRenderer subTile, IPatchRenderer subTileDetail)
        {
            this.fullTileDistantRenderer = fullTileDistant;
            this.fullTileNearRenderer = fullTileNear;
            this.subTileRenderer = subTile;
            this.subTileDetailRenderer = subTileDetail;
            this.DetailRadius = 4.0f;
            this.DistantTileRadius = 1024.0f;
        }

        public void Load()
        {
            // assume child renderers are loaded by caller
        }

        public void Render(TerrainTile tile, Matrix4 projection, Matrix4 view, Vector3 eye)
        {
            var xformeye = Vector3.Transform(eye, tile.InverseModelMatrix);

            // construct our top-level quadtree node
            var root = new QuadTreeNode()
            {
                TopLeft = new QuadTreeVertex()
                {
                    Position = new Vector3(0f, 0f, 0f)
                    //Position = Vector3.Transform(new Vector3(0f, 0f, 0f), tile.ModelMatrix)
                },
                TopRight = new QuadTreeVertex()
                {
                    Position = new Vector3((float)tile.Width, 0f, 0f)
                    //Position = Vector3.Transform(new Vector3((float)tile.Width, 0f, 0f), tile.ModelMatrix)
                },
                BottomLeft = new QuadTreeVertex()
                {
                    Position = new Vector3(0f, 0f, (float)tile.Height)
                    //Position = Vector3.Transform(new Vector3(0f, 0f, (float)tile.Height), tile.ModelMatrix)
                },
                BottomRight = new QuadTreeVertex()
                {
                    Position = new Vector3((float)tile.Width, 0f, (float)tile.Height)
                    //Position = Vector3.Transform(new Vector3((float)tile.Width, 0f, (float)tile.Height), tile.ModelMatrix)
                },
                MinHeight = tile.MinHeight,
                MaxHeight = tile.MaxHeight,
                TileSize = tile.Width // assume width == height
            };

            // check to see if we're completely outside the far detail radius
            if (!root.IsViewerInDetailRange(xformeye, this.DistantTileRadius))
            {
                fullTileDistantRenderer.Render(tile, projection, view, eye);
            }
            else // check inner detail radius
            {
                if (!root.IsViewerInDetailRange(xformeye, this.DetailRadius))
                {
                    // HACK because we're using the same patch renderer as the detail patches and it keeps state
                    (fullTileNearRenderer as IPatchRenderer).Scale = 1.0f;
                    (fullTileNearRenderer as IPatchRenderer).Offset = Vector2.Zero;
                    (fullTileNearRenderer as IPatchRenderer).Width = tile.Width;
                    (fullTileNearRenderer as IPatchRenderer).Height = tile.Height;

                    fullTileNearRenderer.Render(tile, projection, view, eye);
                }
                else
                {
                    root.Render(tile, projection, view, eye, xformeye, this.DetailRadius, this.subTileRenderer, this.subTileDetailRenderer);
                }
            }
        }

        public void Unload()
        {

        }
    }
}
