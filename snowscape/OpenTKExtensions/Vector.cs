using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace OpenTKExtensions
{
    // replacements for some XNA functions
    public static class Vector
    {
        public static Vector3 TopDown(this Vector3 v)
        {
            return new Vector3(v.X, v.Z, 0f);
        }
        public static Vector3 TopDown(this Vector4 v)
        {
            return new Vector3(v.X, v.Z, 0f);
        }

        public static Vector4 ThreePlaneIntersect(Vector4 p1,Vector4 p2,Vector4 p3)
        {
            Vector3 n1 = p1.Xyz.Normalized();
            Vector3 n2 = p2.Xyz.Normalized();
            Vector3 n3 = p3.Xyz.Normalized();
            float d1 = -p1.W;
            float d2 = -p2.W;
            float d3 = -p3.W;


            float denom = Vector3.Dot(n1, Vector3.Cross(n2, n3));
            if (denom == 0.0) return Vector4.Zero;  // HACK

            return new Vector4(
                (
                    Vector3.Cross(n2,n3) * d1 + 
                    Vector3.Cross(n3,n1) * d2 + 
                    Vector3.Cross(n1,n2) * d3 
                ) / denom, 1f);


        }
    }
}
