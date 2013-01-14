using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace OpenTKExtensions
{
    public class Uniform<T>
    {
        public int Location { get; set; }
        public string Name { get; set; }
        public T Value { get; set; }
        public ShaderProgram Program { get; set; }

        public Uniform(ShaderProgram program,string name)
        {
            this.Location = -1;
            this.Program = program;
            this.Name = name;

            this.FindLocation();
        }

        public void FindLocation()
        {
            if (this.Location == -1){
                if (this.Program != null && this.Program.Handle != -1)
                {
                    this.Location = GL.GetUniformLocation(this.Program.Handle, this.Name);
                }
            }
        }

        public void Apply()
        {
            
        }


    }
}
