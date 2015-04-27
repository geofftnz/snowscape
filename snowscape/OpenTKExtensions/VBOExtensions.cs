using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions
{
    public static class VBOExtensions
    {
        public static VBO ToVBO(this IEnumerable<Vector4> data, string name = "vbo", BufferTarget target = BufferTarget.ArrayBuffer) 
        {
            var vbo = new VBO(name, target);
            vbo.SetData(data.ToArray());
            return vbo;
        }
        public static VBO ToVBO(this IEnumerable<Vector3> data, string name = "vbo", BufferTarget target = BufferTarget.ArrayBuffer)
        {
            var vbo = new VBO(name, target);
            vbo.SetData(data.ToArray());
            return vbo;
        }
        public static VBO ToVBO(this IEnumerable<Vector2> data, string name = "vbo", BufferTarget target = BufferTarget.ArrayBuffer)
        {
            var vbo = new VBO(name, target);
            vbo.SetData(data.ToArray());
            return vbo;
        }
        public static VBO ToVBO(this IEnumerable<float> data, string name = "vbo", BufferTarget target = BufferTarget.ArrayBuffer)
        {
            var vbo = new VBO(name, target);
            vbo.SetData(data.ToArray());
            return vbo;
        }
        public static VBO ToVBO(this IEnumerable<byte> data, string name = "vbo", BufferTarget target = BufferTarget.ArrayBuffer)
        {
            var vbo = new VBO(name, target);
            vbo.SetData(data.ToArray());
            return vbo;
        }

        /// <summary>
        /// HACKY GUESS that uints == indices
        /// </summary>
        /// <param name="data"></param>
        /// <param name="name"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static VBO ToVBO(this IEnumerable<uint> data, string name = "vbo", BufferTarget target = BufferTarget.ElementArrayBuffer)
        {
            var vbo = new VBO(name, target);
            vbo.SetData(data.ToArray());
            return vbo;
        }

    }
}
