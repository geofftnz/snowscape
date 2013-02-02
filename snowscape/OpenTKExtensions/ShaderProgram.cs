using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

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

        private Dictionary<string, int> uniformLocations = new Dictionary<string, int>();
        public Dictionary<string, int> UniformLocations
        {
            get
            {
                return this.uniformLocations;
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

        public void ClearUniforms()
        {
            this.uniformLocations.Clear();
        }

        private int LocateUniform(string name)
        {
            int location=0;
            if (this.UniformLocations.TryGetValue(name, out location))
            {
                return location;
            }
            else
            {
                location = GL.GetUniformLocation(this.Handle, name);

                if (location != -1)
                {
                    this.UniformLocations.Add(name, location);
                }
            }
            return location;
        }

        public void SetUniform(string name, float value) 
        {
            int location = LocateUniform(name);
            if (location != -1)
            {
                GL.Uniform1(location, value);
            }
        }
        public void SetUniform(string name, Vector4 value)
        {
            int location = LocateUniform(name);
            if (location != -1)
            {
                GL.Uniform4(location, value);
            }
        }
        public void SetUniform(string name, Matrix4 value)
        {
            int location = LocateUniform(name);
            if (location != -1)
            {
                GL.UniformMatrix4(location,false,ref value);
            }
        }

        //TODO: every other uniform type


    }
}
