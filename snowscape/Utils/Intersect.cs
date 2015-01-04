using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Utils
{
    public static class Intersect
    {
        public static float Sqr(float a)
        {
            return a * a;
        }

        public static bool BoxSphereIntersect(Vector3 b1, Vector3 b2, Vector3 C, float r)
        {
            Vector3 Bmin = new Vector3
                (
                    b1.X < b2.X ? b1.X : b2.X,
                    b1.Y < b2.Y ? b1.Y : b2.Y,
                    b1.Z < b2.Z ? b1.Z : b2.Z
                );
            Vector3 Bmax = new Vector3
                (
                    b1.X < b2.X ? b2.X : b1.X,
                    b1.Y < b2.Y ? b2.Y : b1.Y,
                    b1.Z < b2.Z ? b2.Z : b1.Z
                );

            float r2 = r * r;
            float dmin = 0;

            for (int i = 0; i < 3; i++)
            {
                if (C[i] < Bmin[i])
                    dmin += Sqr(C[i] - Bmin[i]);
                else
                    if (C[i] > Bmax[i])
                        dmin += Sqr(C[i] - Bmax[i]);
            }
            return dmin <= r2;
        }
    }
}
