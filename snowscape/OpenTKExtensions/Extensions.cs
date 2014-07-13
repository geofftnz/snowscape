using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace OpenTKExtensions
{
    public static class Extensions
    {

        public static Vector3 Exp(this Vector3 a)
        {
            return new Vector3((float)Math.Exp(a.X), (float)Math.Exp(a.Y), (float)Math.Exp(a.Z));
        }

        public static Vector4 Exp(this Vector4 a)
        {
            return new Vector4((float)Math.Exp(a.X), (float)Math.Exp(a.Y), (float)Math.Exp(a.Z), (float)Math.Exp(a.W));
        }

        public static Vector3 Sqrt(this Vector3 a)
        {
            return new Vector3((float)Math.Sqrt(a.X), (float)Math.Sqrt(a.Y), (float)Math.Sqrt(a.Z));
        }

        public static Vector4 Sqrt(this Vector4 a)
        {
            return new Vector4((float)Math.Sqrt(a.X), (float)Math.Sqrt(a.Y), (float)Math.Sqrt(a.Z), (float)Math.Sqrt(a.W));
        }

        public static Vector3 Pow(this Vector3 v, float e)
        {
            return new Vector3((float)Math.Pow(v.X, e), (float)Math.Pow(v.Y, e), (float)Math.Pow(v.Z, e));
        }
        public static Vector4 Pow(this Vector4 v, float e)
        {
            return new Vector4((float)Math.Pow(v.X, e), (float)Math.Pow(v.Y, e), (float)Math.Pow(v.Z, e), (float)Math.Pow(v.W, e));
        }
        public static Vector3 Pow(this Vector3 v, Vector3 e)
        {
            return new Vector3((float)Math.Pow(v.X, e.X), (float)Math.Pow(v.Y, e.Y), (float)Math.Pow(v.Z, e.Z));
        }
        public static Vector4 Pow(this Vector4 v, Vector4 e)
        {
            return new Vector4((float)Math.Pow(v.X, e.X), (float)Math.Pow(v.Y, e.Y), (float)Math.Pow(v.Z, e.Z), (float)Math.Pow(v.W, e.W));
        }

        public static Vector2 Abs(this Vector2 v)
        {
            return new Vector2(Math.Abs(v.X), Math.Abs(v.Y));
        }
        public static Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
        }
        public static Vector4 Abs(this Vector4 v)
        {
            return new Vector4(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z), Math.Abs(v.W));
        }

        //public static Vector3 HexColRGB(this string s)
        //{

        //}

    }
}
