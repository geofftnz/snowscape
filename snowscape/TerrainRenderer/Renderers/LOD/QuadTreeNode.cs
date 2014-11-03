using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using U = Utils.Utils;
using Utils;

namespace Snowscape.TerrainRenderer.Renderers.LOD
{
    public class QuadTreeNode
    {

        public enum Corner
        {
            BottomNearLeft = 0,
            BottomNearRight,
            BottomFarLeft,
            BottomFarRight,
            TopNearLeft,
            TopNearRight,
            TopFarLeft,
            TopFarRight
        }

        private TerrainTile tile;

        /// <summary>
        /// Bounding box corners. Transformed and in world space
        /// </summary>
        private Vector4[] vertex = new Vector4[8];
        private Vector4 BottomCentre, TopCentre;

        public float MinHeight { get { return vertex[(int)Corner.BottomNearLeft].Y; } }
        public float MaxHeight { get { return vertex[(int)Corner.TopNearLeft].Y; } }

        private int tileSize;

        // distance and LOD of 4 corners + closest point to viewer
        public float[] distance = { 0f, 0f, 0f, 0f, 0f };
        public int[] lod = { 0, 0, 0, 0, 0 };

        public int depth = 0;

        bool checkFrustum = true;

        //private Vector3 MinVertex;
        //private Vector3 MaxVertex;

        public QuadTreeNode(TerrainTile tile, int tileSize, int depth, bool checkFrustum, params Vector4[] v)
        {
            this.tile = tile;
            this.checkFrustum = checkFrustum;
            this.depth = depth;
            this.tileSize = tileSize;

            for (int i = 0; i < 8; i++)
            {
                vertex[i] = v[i];
            }

            BottomCentre = (vertex[(int)Corner.BottomNearLeft] + vertex[(int)Corner.BottomFarRight]) * 0.5f;
            TopCentre = (vertex[(int)Corner.TopNearLeft] + vertex[(int)Corner.TopFarRight]) * 0.5f;
        }

        public QuadTreeNode(TerrainTile tile)
            : this(tile, tile.Width, 0, true, tile.BoundingBox.Select(v => Vector4.Transform(v, tile.ModelMatrix)).ToArray())
        {
        }

        public IEnumerable<PatchDescriptor> GetPatches(Vector3 eye, Frustum f)
        {
            bool childFrustumCheck = true;

            // frustum cull
            if (checkFrustum)
            {
                var clipresult = f.TestBox(this.vertex);
                if (clipresult == Frustum.ObjectClipResult.TotallyOutside)
                {
                    yield break;
                }
                if (clipresult == Frustum.ObjectClipResult.TotallyInside)
                {
                    childFrustumCheck = false;
                }
            }

            // get eye y-difference (0 within min/max height)

            //float eyeY = U.Max(U.Max(eye.Y - MaxHeight, 0f), U.Max(MinHeight - eye.Y, 0f));
            float eyeYdiff = 0f;
            if (eye.Y > MaxHeight)
            {
                eyeYdiff = eye.Y - MaxHeight;
            }
            else if (eye.Y < MinHeight)
            {
                eyeYdiff = MinHeight - eye.Y;
            }

            eyeYdiff *= eyeYdiff;

            int i = 0;
            distance[i++] = (float)Math.Sqrt((eye.X - vertex[i].X).Sqr() + eyeYdiff + (eye.Z - vertex[i].Z).Sqr());
            distance[i++] = (float)Math.Sqrt((eye.X - vertex[i].X).Sqr() + eyeYdiff + (eye.Z - vertex[i].Z).Sqr());
            distance[i++] = (float)Math.Sqrt((eye.X - vertex[i].X).Sqr() + eyeYdiff + (eye.Z - vertex[i].Z).Sqr());
            distance[i++] = (float)Math.Sqrt((eye.X - vertex[i].X).Sqr() + eyeYdiff + (eye.Z - vertex[i].Z).Sqr());
            distance[i++] = U.DistanceToSquare(
                                vertex[(int)Corner.BottomNearLeft].X, vertex[(int)Corner.BottomNearLeft].Z,
                                vertex[(int)Corner.BottomFarRight].X, vertex[(int)Corner.BottomFarRight].Z,
                                eye.X, eye.Z,
                                eyeYdiff);

            for (i = 0; i < 5; i++)
            {
                lod[i] = GetLOD(distance[i], depth);
            }

            int patchLOD = lod.Max();
            int LODDiff = patchLOD - lod.Min();
            float patchDistance = distance.Min();

            // recusion limit, or not enough lod difference
            if (depth > 6 || LODDiff < 2)
            {
                yield return new PatchDescriptor
                {
                    Tile = this.tile,
                    TileModelMatrix = this.tile.ModelMatrix,
                    TileSize = tileSize,
                    MeshSize = GetMeshSize(tileSize, patchLOD),
                    Offset = vertex[(int)Corner.BottomNearLeft].Xz,// / (float)this.tile.Width,
                    Distance = patchDistance
                };
            }
            else
            {
                // recurse

                var BottomNearCentre = vertex[(int)Corner.BottomNearLeft];
                BottomNearCentre.X = BottomCentre.X;
                var BottomCentreLeft = vertex[(int)Corner.BottomNearLeft];
                BottomCentreLeft.Z = BottomCentre.Z;
                var BottomFarCentre = vertex[(int)Corner.BottomFarLeft];
                BottomFarCentre.X = BottomCentre.X;
                var BottomCentreRight = vertex[(int)Corner.BottomNearRight];
                BottomCentreRight.Z = BottomCentre.Z;

                var TopNearCentre = vertex[(int)Corner.TopNearLeft];
                TopNearCentre.X = TopCentre.X;
                var TopCentreLeft = vertex[(int)Corner.TopNearLeft];
                TopCentreLeft.Z = TopCentre.Z;
                var TopFarCentre = vertex[(int)Corner.TopFarLeft];
                TopFarCentre.X = TopCentre.X;
                var TopCentreRight = vertex[(int)Corner.TopNearRight];
                TopCentreRight.Z = TopCentre.Z;

                // near left quadrant
                foreach (var pd in new QuadTreeNode(tile, tileSize >> 1, depth + 1, childFrustumCheck,
                    vertex[(int)Corner.BottomNearLeft],
                    BottomNearCentre,
                    BottomCentreLeft,
                    BottomCentre,
                    vertex[(int)Corner.TopNearLeft],
                    TopNearCentre,
                    TopCentreLeft,
                    TopCentre
                    ).GetPatches(eye, f))
                {
                    yield return pd;
                }

                // near right quadrant
                foreach (var pd in new QuadTreeNode(tile, tileSize >> 1, depth + 1, childFrustumCheck,
                    BottomNearCentre,
                    vertex[(int)Corner.BottomNearRight],
                    BottomCentre,
                    BottomCentreRight,
                    TopNearCentre,
                    vertex[(int)Corner.TopNearRight],
                    TopCentre,
                    TopCentreRight
                    ).GetPatches(eye, f))
                {
                    yield return pd;
                }

                // far left quadrant
                foreach (var pd in new QuadTreeNode(tile, tileSize >> 1, depth + 1, childFrustumCheck,
                    BottomCentreLeft,
                    BottomCentre,
                    vertex[(int)Corner.BottomFarLeft],
                    BottomFarCentre,
                    TopCentreLeft,
                    TopCentre,
                    vertex[(int)Corner.TopFarLeft],
                    TopFarCentre
                    ).GetPatches(eye, f))
                {
                    yield return pd;
                }

                // far right quadrant
                foreach (var pd in new QuadTreeNode(tile, tileSize >> 1, depth + 1, childFrustumCheck,
                    BottomCentre,
                    BottomCentreRight,
                    BottomFarCentre,
                    vertex[(int)Corner.BottomFarRight],
                    TopCentre,
                    TopCentreRight,
                    TopFarCentre,
                    vertex[(int)Corner.TopFarRight]
                    ).GetPatches(eye, f))
                {
                    yield return pd;
                }
            }

        }

        public static int GetLOD(float distance, int depth)
        {
            if (distance < 8f && depth > 3) return 4;
            if (distance < 16f && depth > 2) return 3;
            if (distance < 32f && depth > 1) return 2;
            if (distance < 64f && depth > 0) return 1;
            if (distance < 128f) return 0;
            if (distance < 256f) return -1;
            if (distance < 512f) return -2;
            if (distance < 1024f) return -3;
            return -4;
        }

        public static int GetMeshSize(int tileSize, int LOD)
        {
            if (LOD < 0)
            {
                return tileSize >> (-LOD);
            }
            if (LOD > 0)
            {
                return tileSize << LOD;
            }
            return tileSize;
        }






    }
}
