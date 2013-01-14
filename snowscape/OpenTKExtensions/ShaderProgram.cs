using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace OpenTKExtensions
{
    public class ShaderProgram
    {
        public int Handle { get; set; }
        public string Log { get; set; }

        private List<Shader> shaders = new List<Shader>();
        public List<Shader> Shaders
        {
            get
            {
                return this.shaders;
            }
        }

        public ShaderProgram()
        {
            this.Handle = -1;
        }

        public void Create()
        {
            this.Handle = GL.CreateProgram();
        }

        public void AddShader(Shader s)
        {
            this.Shaders.Add(s);
        }

        public void Link()
        {
            if (this.Handle < 0)
            {
                throw new InvalidOperationException("Program not created");
            }
            if (this.Shaders.Count < 1)
            {
                throw new InvalidOperationException("No shaders added");
            }

            foreach (var s in this.Shaders)
            {
                GL.AttachShader(this.Handle, s.Handle);
            }
            GL.LinkProgram(this.Handle);
            this.Log = GL.GetProgramInfoLog(this.Handle); 
        }

        public void UseProgram()
        {
            GL.UseProgram(this.Handle);
        }


    }
}
