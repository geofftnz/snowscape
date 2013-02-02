using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using NLog;

namespace OpenTKExtensions
{
    public class ShaderProgram
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

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

        private Dictionary<string, int> variableLocations = new Dictionary<string, int>();
        public Dictionary<string, int> VariableLocations
        {
            get
            {
                return this.variableLocations;
            }
        }


        public ShaderProgram()
        {
            this.Handle = -1;
        }

        public void Create()
        {
            if (this.Handle == -1)
            {
                int handle = GL.CreateProgram();

                if (handle != -1)
                {
                    this.Handle = handle;
                }
                else
                {
                    log.Error("ShaderProgram.Create: could not create program.");
                }
            }
        }

        public void AddShader(Shader s)
        {
            this.Shaders.Add(s);
        }

        public void AddShader(ShaderType type, string source)
        {
            Shader s = new Shader();
            s.Type = type;
            s.Source = source;
            s.Create();
            s.Compile();
            this.Shaders.Add(s);
        }

        public void AddVertexShader(string source)
        {
            AddShader(ShaderType.VertexShader, source);
        }
        public void AddFragmentShader(string source)
        {
            AddShader(ShaderType.FragmentShader, source);
        }

        public void AddVariable(int index, string name)
        {
            if (this.VariableLocations.ContainsKey(name))
            {
                this.VariableLocations[name] = index;
            }
            else
            {
                this.VariableLocations.Add(name, index);
            }
        }

        public int VariableLocation(string name)
        {
            int location;
            if (this.VariableLocations.TryGetValue(name, out location))
            {
                return location;
            }
            throw new InvalidOperationException(string.Format("Could not find variable {0}",name));
        }

        public void Link()
        {
            this.Create();

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

            // bind attribs
            foreach (var v in this.VariableLocations)
            {
                GL.BindAttribLocation(this.Handle,v.Value,v.Key);
            }


            GL.LinkProgram(this.Handle);
            
            log.Trace(GL.GetProgramInfoLog(this.Handle));
        }

        public void Init(string vertexSource, string fragmentSource, IList<Variable> variables)
        {
            this.AddVertexShader(vertexSource);
            this.AddFragmentShader(fragmentSource);

            foreach (var v in variables)
            {
                this.AddVariable(v.Index,v.Name);
            }

            this.Link();
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
                    log.Trace("ShaderProgram.LocateUniform: {0} is at {1}", name, location);
                    this.UniformLocations.Add(name, location);
                }
                else
                {
                    log.Warn("ShaderProgram.LocateUniform: Could not locate {0}", name);
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
