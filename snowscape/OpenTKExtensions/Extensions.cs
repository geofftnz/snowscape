﻿using System;
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
    }
}
