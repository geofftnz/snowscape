using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Microsoft.Xna.Framework;
using OpenTK;
using System.Diagnostics;
using OpenTK.Graphics;
using System.IO;

namespace Utils
{
    public static class Utils
    {
        public static T ClampInclusive<T>(this T x, T min, T max) where T : IComparable<T>
        {
            return (x.CompareTo(min) >= 0) ? ((x.CompareTo(max) <= 0) ? x : max) : min;
        }

        public static float Min(float a, float b)
        {
            return (a + b - Math.Abs(a - b)) * 0.5f;
        }
        public static float Max(float a, float b)
        {
            return (a + b + Math.Abs(a - b)) * 0.5f;
        }
        public static float Clamp(this float x, float min, float max)
        {
            if (x < min) return min;
            if (x > max) return max;
            return x;
        }

        public static Color4 ToColor(this Vector3 v)
        {
            return new Color4(v.X * 0.5f + 0.5f, v.Y * 0.5f + 0.5f, v.Z * 0.5f + 0.5f, 1.0f);
        }

        public static Color4 NormalToSphericalColor(this Vector3 v)
        {
            //float r = (float)Math.Sqrt(v.X*v.X+ v.Y*v.Y+ v.Z*v.Z);
            float theta = (float)(Math.Acos(v.Y) / Math.PI);
            float rho = (float)((Math.Atan2(v.Z, v.X) + Math.PI) / (Math.PI));

            return new Color4(theta, rho, 0f, 1f);
        }

        public static float Wrap(this float x, float max)
        {
            x = (float)Math.IEEERemainder(x, max);
            if (x < 0) x += max;
            return x;
        }

        public static int Wrap(this int x, int max)
        {
            x %= max;
            if (x < 0) x += max;
            return x;
        }

        public static double TimeFor(Action a)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            a();
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }

        public static double AverageTime(Func<double> f, int runs)
        {
            if (runs < 1) return 0.0;
            double totalms = 0.0;
            for (int i = 0; i < runs; i++)
            {
                totalms += f();
            }
            return totalms / (double)runs;
        }

        public static float Lerp(this float x, float a, float b)
        {
            return a + (b - a) * x;
        }

        public static string Load(this string filename)
        {
            return File.ReadAllText(filename);
        }

        public static byte UnitToByte(this float f)
        {
            return (byte)((f * 127.0f) + 128.0f).Clamp(0.0f, 255.0f);
        }

        public static float[] Normalize(this float[] x)
        {
            float xmin = x.Min();
            float xmax = x.Max();
            float scale = xmax - xmin;
            if (scale != 0.0f)
            {
                scale = 1.0f / scale;
            }

            for (int i = 0; i < x.Length; i++)
            {
                x[i] -= xmin;
                x[i] *= scale;
            }

            return x;
        }


        public static float DistanceToSquare(float x1, float y1, float x2, float y2, float px, float py)
        {
            if (px >= x1 && px <= x2 && py >= y1 && py <= y2)
            {
                return 0f;
            }

            float cx = (x1 + x2) * 0.5f;
            float cy = (y1 + y2) * 0.5f;
            float rx = Math.Abs(x2 - x1) * 0.5f;
            float ry = Math.Abs(y2 - y1) * 0.5f;

            float nx = Math.Abs(px - cx) - rx;
            float ny = Math.Abs(py - cy) - ry;

            float nx2 = Math.Max(nx, 0.0f);
            float ny2 = Math.Max(ny, 0.0f);

            return Utils.Min(Utils.Max(nx, ny), 0f) + (float)Math.Sqrt(nx2 * nx2 + ny2 * ny2);

            //return -1f;
        }

        /// <summary>
        /// distance to square, with a supplied 3rd coordinate difference.
        /// 
        /// Only used in quadtree
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="px"></param>
        /// <param name="py"></param>
        /// <param name="hdiff"></param>
        /// <returns></returns>
        public static float DistanceToSquare(float x1, float y1, float x2, float y2, float px, float py, float hdiffsq)
        {
            if (px >= x1 && px <= x2 && py >= y1 && py <= y2)
            {
                return 0f;
            }

            float cx = (x1 + x2) * 0.5f;
            float cy = (y1 + y2) * 0.5f;
            float rx = Math.Abs(x2 - x1) * 0.5f;
            float ry = Math.Abs(y2 - y1) * 0.5f;

            float nx = Math.Abs(px - cx) - rx;
            float ny = Math.Abs(py - cy) - ry;

            float nx2 = Math.Max(nx, 0.0f);
            float ny2 = Math.Max(ny, 0.0f);

            return Utils.Min(Utils.Max(nx, ny), 0f) + (float)Math.Sqrt(nx2 * nx2 + ny2 * ny2 + hdiffsq);

            //return -1f;
        }

        public static float Sqr(this float x)
        {
            return x * x;
        }

    }
}
