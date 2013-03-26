using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using OpenTK.Graphics.OpenGL;

namespace OpenTKExtensions
{
    public class Sampler
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public int ID { get; private set; }
        public string Name { get; private set; }

        private Dictionary<SamplerParameter, ISamplerParameter> parameters = new Dictionary<SamplerParameter, ISamplerParameter>();
        public Dictionary<SamplerParameter, ISamplerParameter> Parameters { get { return this.parameters; } }

        public Sampler(string name)
        {
            this.Name = name;
            this.ID = -1;
        }
        public Sampler()
            : this("unnamed")
        {
        }

        public int Init()
        {
            if (this.ID == -1)
            {
                this.ID = GL.GenSampler();
                log.Trace("Sampler.GenerateID ({0}) returned {1}", this.Name, this.ID);
            }
            return this.ID;
        }

        public void Delete()
        {
            if (this.ID != -1)
            {
                GL.DeleteSampler(this.ID);
            }
        }

        public void ApplyParameters()
        {
            if (this.Init() != -1)
            {
                foreach (var param in this.Parameters.Values)
                {
                    param.Apply((uint)this.ID);
                }
            }
        }

        public Sampler SetParameter(ISamplerParameter param)
        {
            if (this.Parameters.ContainsKey(param.ParameterName))
            {
                this.Parameters[param.ParameterName] = param;
            }
            else
            {
                this.Parameters.Add(param.ParameterName, param);
            }
            return this;
        }

        public Sampler Bind(TextureUnit textureUnit)
        {
            return Bind(textureUnit - TextureUnit.Texture0);
        }
        public Sampler Bind(int textureUnit)
        {
            if (this.Init() != -1)
            {
                GL.BindSampler(textureUnit, this.ID);
            }
            return this;
        }
        public static void Unbind(int textureUnit)
        {
            GL.BindSampler(textureUnit, 0);
        }
        public static void Unbind(TextureUnit textureUnit)
        {
            Unbind(textureUnit - TextureUnit.Texture0);
        }
    }
}
