using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using NLog;

namespace OpenTKExtensions
{
    public class Shader
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public int Handle { get; set; }
        public string Source { get; set; }
        public ShaderType Type { get; set; }

        public Shader()
        {
            this.Type = ShaderType.FragmentShader;
            this.Handle = -1;
            this.Source = string.Empty;
        }

        public int Init()
        {
            if (this.Handle == -1)
            {
                this.Handle = GL.CreateShader(this.Type);
            }
            return this.Handle;
        }

        public void Compile()
        {
            if (this.Init() != -1)
            {
                GL.ShaderSource(this.Handle, this.Source);
                GL.CompileShader(this.Handle);

                string infoLog = GL.GetShaderInfoLog(this.Handle);

                int compileStatus;
                GL.GetShader(this.Handle, ShaderParameter.CompileStatus, out compileStatus);

                if (compileStatus != 1)
                {
                    log.Error("Shader.Compile: {0}", infoLog);
                    throw new InvalidOperationException(string.Format("Shader did not compile: {0}", infoLog));
                }
                else
                {
                    log.Trace("Shader.Compile: {0}", infoLog);
                }
            }
        }

        





    }
}
