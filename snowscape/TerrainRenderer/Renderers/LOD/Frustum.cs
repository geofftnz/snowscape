using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTKExtensions;

namespace Snowscape.TerrainRenderer.Renderers.LOD
{
    public class Frustum
    {
        public enum ClippingPlane
        {
            Near = 0,
            Far,
            Left,
            Right,
            Top,
            Bottom
        }

        public enum PointClipResult
        {
            Outside = -1,
            Inside = 1
        }

        public enum ObjectClipResult
        {
            TotallyOutside = 0,
            PartiallyInside,
            TotallyInside
        }

        private Vector4[] clippingPlane = new Vector4[6];
        private Vector4[] corner = new Vector4[8];

        public Vector4 LeftPlane { get { return clippingPlane[(int)ClippingPlane.Left]; } }
        public Vector4 RightPlane { get { return clippingPlane[(int)ClippingPlane.Right]; } }
        public Vector4 TopPlane { get { return clippingPlane[(int)ClippingPlane.Top]; } }
        public Vector4 BottomPlane { get { return clippingPlane[(int)ClippingPlane.Bottom]; } }
        public Vector4 NearPlane { get { return clippingPlane[(int)ClippingPlane.Near]; } }
        public Vector4 FarPlane { get { return clippingPlane[(int)ClippingPlane.Far]; } }

        public Vector4 NearTopLeftCorner { get { return corner[0]; } }
        public Vector4 NearBottomLeftCorner { get { return corner[1]; } }
        public Vector4 NearTopRightCorner { get { return corner[2]; } }
        public Vector4 NearBottomRightCorner { get { return corner[3]; } }

        public Vector4 FarTopLeftCorner { get { return corner[4]; } }
        public Vector4 FarBottomLeftCorner { get { return corner[5]; } }
        public Vector4 FarTopRightCorner { get { return corner[6]; } }
        public Vector4 FarBottomRightCorner { get { return corner[7]; } }


        public Frustum(Matrix4 viewProjection)
        {
            ExtractPlanes(viewProjection);
            SetCorners();
        }

        private void SetCorners()
        {
            int i = 0;

            corner[i++] = Vector.ThreePlaneIntersect(NearPlane, LeftPlane, TopPlane);
            corner[i++] = Vector.ThreePlaneIntersect(NearPlane, LeftPlane, BottomPlane);
            corner[i++] = Vector.ThreePlaneIntersect(NearPlane, RightPlane, TopPlane);
            corner[i++] = Vector.ThreePlaneIntersect(NearPlane, RightPlane, BottomPlane);
            corner[i++] = Vector.ThreePlaneIntersect(FarPlane, LeftPlane, TopPlane);
            corner[i++] = Vector.ThreePlaneIntersect(FarPlane, LeftPlane, BottomPlane);
            corner[i++] = Vector.ThreePlaneIntersect(FarPlane, RightPlane, TopPlane);
            corner[i++] = Vector.ThreePlaneIntersect(FarPlane, RightPlane, BottomPlane);
        }

        private int PointPlane(Vector4 plane, Vector4 point)
        {
            return Vector4.Dot(plane, point) < 0f ? -1 : 1;
        }

        /// <summary>
        /// returns the sign of the distance
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public PointClipResult TestPoint(Vector4 p)
        {
            for (int i = 0; i < 6; i++)
            {
                if (PointPlane(clippingPlane[i], p) < 0)
                {
                    return PointClipResult.Outside;
                }
            }
            return PointClipResult.Inside;
        }

        public PointClipResult TestPoint(Vector3 p)
        {
            return TestPoint(new Vector4(p, 1.0f));
        }

        /// <summary>
        /// http://www.iquilezles.org/www/articles/frustumcorrect/frustumcorrect.htm
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        public ObjectClipResult TestBox(Vector4[] ps)
        {
            float mMinX, mMinY, mMinZ, mMaxX, mMaxY, mMaxZ;

            mMinX = ps.Select(v => v.X).Min();
            mMinY = ps.Select(v => v.Y).Min();
            mMinZ = ps.Select(v => v.Z).Min();
            mMaxX = ps.Select(v => v.X).Max();
            mMaxY = ps.Select(v => v.Y).Max();
            mMaxZ = ps.Select(v => v.Z).Max();

            // check box outside/inside of frustum
            int boxout;
            bool boxpartial = false;

            for (int i = 0; i < 6; i++)
            {
                boxout = 0;
                boxout += ((Vector4.Dot(clippingPlane[i], new Vector4(mMinX, mMinY, mMinZ, 1.0f)) < 0.0) ? 1 : 0);
                boxout += ((Vector4.Dot(clippingPlane[i], new Vector4(mMaxX, mMinY, mMinZ, 1.0f)) < 0.0) ? 1 : 0);
                boxout += ((Vector4.Dot(clippingPlane[i], new Vector4(mMinX, mMaxY, mMinZ, 1.0f)) < 0.0) ? 1 : 0);
                boxout += ((Vector4.Dot(clippingPlane[i], new Vector4(mMaxX, mMaxY, mMinZ, 1.0f)) < 0.0) ? 1 : 0);
                boxout += ((Vector4.Dot(clippingPlane[i], new Vector4(mMinX, mMinY, mMaxZ, 1.0f)) < 0.0) ? 1 : 0);
                boxout += ((Vector4.Dot(clippingPlane[i], new Vector4(mMaxX, mMinY, mMaxZ, 1.0f)) < 0.0) ? 1 : 0);
                boxout += ((Vector4.Dot(clippingPlane[i], new Vector4(mMinX, mMaxY, mMaxZ, 1.0f)) < 0.0) ? 1 : 0);
                boxout += ((Vector4.Dot(clippingPlane[i], new Vector4(mMaxX, mMaxY, mMaxZ, 1.0f)) < 0.0) ? 1 : 0);
                if (boxout == 8) return ObjectClipResult.TotallyOutside;
                if (boxout > 0) boxpartial = true;
            }

            if (!boxpartial) return ObjectClipResult.TotallyInside;

            int boxout2;
            boxout2=0; for( int i=0; i<8; i++ ) boxout2 += ((corner[i].X > mMaxX)?1:0); if( boxout2==8 ) return ObjectClipResult.TotallyOutside;
            boxout2=0; for( int i=0; i<8; i++ ) boxout2 += ((corner[i].X < mMinX)?1:0); if( boxout2==8 ) return ObjectClipResult.TotallyOutside;
            boxout2=0; for( int i=0; i<8; i++ ) boxout2 += ((corner[i].Y > mMaxY)?1:0); if( boxout2==8 ) return ObjectClipResult.TotallyOutside;
            boxout2=0; for( int i=0; i<8; i++ ) boxout2 += ((corner[i].Y < mMinY)?1:0); if( boxout2==8 ) return ObjectClipResult.TotallyOutside;
            boxout2=0; for( int i=0; i<8; i++ ) boxout2 += ((corner[i].Z > mMaxZ)?1:0); if( boxout2==8 ) return ObjectClipResult.TotallyOutside;
            boxout2=0; for( int i=0; i<8; i++ ) boxout2 += ((corner[i].Z < mMinZ)?1:0); if( boxout2==8 ) return ObjectClipResult.TotallyOutside;


            return ObjectClipResult.PartiallyInside;
        }

        /*
         *   0  1  2  3 
         *   4  5  6  7
         *   8  9 10 11
         *  12 13 14 15
         * 
         *   0  4  8 12
         *   1  5  9 13
         *   2  6 10 14
         *   3  7 11 15
         *   
         * 
         */
        private void ExtractPlanes(Matrix4 m)
        {
            clippingPlane[(int)ClippingPlane.Left] = new Vector4(
                m.M14 + m.M11,
                m.M24 + m.M21,
                m.M34 + m.M31,
                m.M44 + m.M41);

            clippingPlane[(int)ClippingPlane.Right] = new Vector4(
                m.M14 - m.M11,
                m.M24 - m.M21,
                m.M34 - m.M31,
                m.M44 - m.M41);

            clippingPlane[(int)ClippingPlane.Bottom] = new Vector4(
                m.M14 + m.M12,
                m.M24 + m.M22,
                m.M34 + m.M32,
                m.M44 + m.M42);

            clippingPlane[(int)ClippingPlane.Top] = new Vector4(
                m.M14 - m.M12,
                m.M24 - m.M22,
                m.M34 - m.M32,
                m.M44 - m.M42);

            clippingPlane[(int)ClippingPlane.Near] = new Vector4(
                m.M13,
                m.M23,
                m.M33,
                m.M43);

            clippingPlane[(int)ClippingPlane.Far] = new Vector4(
                m.M14 - m.M13,
                m.M24 - m.M23,
                m.M34 - m.M33,
                m.M44 - m.M43);

            // normalize off xyz only.
            for (int i = 0; i < 6; i++)
            {
                clippingPlane[i] /= clippingPlane[i].Xyz.Length;
            }

        }


    }
}
