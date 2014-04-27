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

        private static IShaderLoader _defaultLoader = null;
        public static IShaderLoader DefaultLoader
        {
            get { return _defaultLoader;  }
            set { _defaultLoader = value; }
        }

        public int Handle { get; set; }
        public string Log { get; set; }
        public string Name { get; set; }

        private List<Shader> shaders = new List<Shader>();
        public List<Shader> Shaders
        {
            get
            {
                return this.shaders;
            }
        }

        private Dictionary<string, int> uniformLocations = new Dictionary<string, int>();
        public Dictionary<string, int> UniformLocations { get { return this.uniformLocations; } }

        private Dictionary<string, int> variableLocations = new Dictionary<string, int>();
        public Dictionary<string, int> VariableLocations { get { return this.variableLocations; } }

        private Dictionary<int, string> fragDataLocation = new Dictionary<int, string>();
        public Dictionary<int, string> FragDataLocation { get { return this.fragDataLocation; } }
        

        public ShaderProgram(string name)
        {
            this.Handle = -1;
            this.Name = name;
        }

        public ShaderProgram()
            : this("unnamed")
        {

        }

        public ShaderProgram Create()
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
                    log.Error("ShaderProgram.Create ({0}): could not create program.", this.Name);
                }
            }
            return this;
        }

        public ShaderProgram AddShader(Shader s)
        {
            this.Shaders.Add(s);
            return this;
        }

        public ShaderProgram AddShader(ShaderType type, string source)
        {
            Shader s = new Shader(string.Format("{0}:{1}", this.Name, type.ToString()));
            s.Type = type;
            s.Source = source;
            s.Compile();
            this.Shaders.Add(s);
            return this;
        }

        public ShaderProgram AddVertexShader(string source)
        {
            AddShader(ShaderType.VertexShader, source);
            return this;
        }
        public ShaderProgram AddFragmentShader(string source)
        {
            AddShader(ShaderType.FragmentShader, source);
            return this;
        }

        public ShaderProgram AddVariable(int index, string name)
        {
            if (this.VariableLocations.ContainsKey(name))
            {
                this.VariableLocations[name] = index;
            }
            else
            {
                this.VariableLocations.Add(name, index);
            }
            return this;
        }

        public ShaderProgram AddFragmentShaderOutput(int colourIndex, string name)
        {
            if (this.FragDataLocation.ContainsKey(colourIndex))
            {
                this.FragDataLocation[colourIndex] = name;
            }
            else
            {
                this.FragDataLocation.Add(colourIndex, name);
            }
            return this;
        }


        public int VariableLocation(string name)
        {
            int location;
            if (this.VariableLocations.TryGetValue(name, out location))
            {
                return location;
            }
            throw new InvalidOperationException(string.Format("Shader Program {0} could not find variable {1}", this.Name, name));
        }

        private void BindFragDataLocation(int colourSlot, string outputName)
        {
            GL.BindFragDataLocation(this.Handle, colourSlot, outputName);
        }

        public void Link()
        {
            this.Create();

            if (this.Handle < 0)
            {
                throw new InvalidOperationException(string.Format("Shader Program {0} Link: program was not created before Link() called", this.Name));
            }
            if (this.Shaders.Count < 1)
            {
                throw new InvalidOperationException(string.Format("Shader Program {0} Link: no shaders were added prior to linking", this.Name));
            }

            foreach (var s in this.Shaders)
            {
                GL.AttachShader(this.Handle, s.Handle);
            }

            // bind attribs
            foreach (var v in this.VariableLocations)
            {
                GL.BindAttribLocation(this.Handle, v.Value, v.Key);
            }

            // bind frag data outputs (for MRT)
            foreach (var colindex in this.FragDataLocation.Keys)
            {
                GL.BindFragDataLocation(this.Handle, colindex, this.FragDataLocation[colindex]);
            }


            GL.LinkProgram(this.Handle);

            string infoLog = GL.GetProgramInfoLog(this.Handle).TrimEnd();
            int linkStatus;
            GL.GetProgram(this.Handle, ProgramParameter.LinkStatus, out linkStatus);

            if (linkStatus != 1)
            {
                log.Error("ShaderProgram.Link ({0}): {1}", this.Name, infoLog);
                throw new InvalidOperationException(string.Format("ShaderProgram.Link ({0}): {1}", this.Name, infoLog));
            }
            else
            {
                log.Trace("ShaderProgram.Link ({0}): {1}", this.Name, infoLog);
            }

        }

        public ShaderProgram Init(string vertexSource, string fragmentSource, IList<Variable> variables, string[] fragDataOutputs, IShaderLoader loader)
        {
            string vsrc, fsrc;

            if (loader != null)
            {
                vsrc = loader.Load(vertexSource);
                fsrc = loader.Load(fragmentSource);
            }
            else  // source is raw
            {
                vsrc = vertexSource;
                fsrc = fragmentSource;
            }

            this.AddVertexShader(vertexSource);
            this.AddFragmentShader(fragmentSource);

            foreach (var v in variables)
            {
                this.AddVariable(v.Index, v.Name);
            }

            if (fragDataOutputs != null)
            {
                for (int i = 0; i < fragDataOutputs.Length; i++)
                {
                    this.AddFragmentShaderOutput(i, fragDataOutputs[i]);
                }
            }

            this.Link();
            return this;
        }


        public ShaderProgram Init(string vertexSource, string fragmentSource, IList<Variable> variables, string[] fragDataOutputs)
        {
            return Init(vertexSource, fragmentSource, variables, fragDataOutputs, DefaultLoader);
        }

        public ShaderProgram Init(string vertexSource, string fragmentSource, IList<Variable> variables)
        {
            return Init(vertexSource, fragmentSource, variables, null);
        }



        public ShaderProgram UseProgram()
        {
            GL.UseProgram(this.Handle);
            return this;
        }

        public ShaderProgram ClearUniforms()
        {
            this.uniformLocations.Clear();
            return this;
        }

        private int LocateUniform(string name)
        {
            int location = 0;
            if (this.UniformLocations.TryGetValue(name, out location))
            {
                return location;
            }
            else
            {
                location = GL.GetUniformLocation(this.Handle, name);

                if (location != -1)
                {
                    log.Trace("ShaderProgram.LocateUniform ({0}): {1} is at {2}", this.Name, name, location);
                    this.UniformLocations.Add(name, location);
                }
                else
                {
                    log.Trace("ShaderProgram.LocateUniform ({0}): Could not locate {1}", this.Name, name);
                }
            }
            return location;
        }

        public ShaderProgram SetUniform(string name, float value)
        {
            int location = LocateUniform(name);
            if (location != -1)
            {
                GL.Uniform1(location, value);
            }
            return this;
        }
        public ShaderProgram SetUniform(string name, int value)
        {
            int location = LocateUniform(name);
            if (location != -1)
            {
                GL.Uniform1(location, value);
            }
            return this;
        }
        public ShaderProgram SetUniform(string name, Vector2 value)
        {
            int location = LocateUniform(name);
            if (location != -1)
            {
                GL.Uniform2(location, value);
            }
            return this;
        }
        public ShaderProgram SetUniform(string name, Vector3 value)
        {
            int location = LocateUniform(name);
            if (location != -1)
            {
                GL.Uniform3(location, value);
            }
            return this;
        }
        public ShaderProgram SetUniform(string name, Vector4 value)
        {
            int location = LocateUniform(name);
            if (location != -1)
            {
                GL.Uniform4(location, value);
            }
            return this;
        }
        public ShaderProgram SetUniform(string name, Matrix4 value)
        {
            int location = LocateUniform(name);
            if (location != -1)
            {
                GL.UniformMatrix4(location, false, ref value);
            }
            return this;
        }

        //TODO: every other uniform type


    }
}
