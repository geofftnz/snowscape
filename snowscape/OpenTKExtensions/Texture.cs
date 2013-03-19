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
    public class Texture
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public int ID { get; private set; }
        public TextureTarget Target { get; private set; }
        public PixelInternalFormat InternalFormat { get; private set; }
        public PixelFormat Format { get; set; }
        public PixelType Type { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public string Name { get; set; }

        private List<ITextureParameter> parameters = new List<ITextureParameter>();
        public List<ITextureParameter> Parameters { get { return this.parameters; } }

        public Texture(string name, int width, int height, TextureTarget target, PixelInternalFormat internalFormat, PixelFormat format, PixelType type)
        {
            this.Name = name;
            this.ID = -1;
            this.Target = target;
            this.InternalFormat = internalFormat;
            this.Width = width;
            this.Height = height;
            this.Format = format;
            this.Type = type;
        }

        public Texture(int width, int height, TextureTarget target, PixelInternalFormat internalFormat, PixelFormat format, PixelType type)
            : this("unnamed", width, height, target, internalFormat, format, type)
        {
        }

        public Texture SetParameter(ITextureParameter param)
        {
            this.Parameters.Add(param);
            return this;
        }

        public int Init()
        {
            if (this.ID == -1)
            {
                this.ID = GL.GenTexture();
                log.Trace("Texture.GenerateID ({0}) returned {1}", this.Name, this.ID);
            }
            return this.ID;
        }

        public void ApplyParameters()
        {
            foreach (var param in this.Parameters)
            {
                param.Apply(this.Target);
            }
        }

        public Texture Bind()
        {
            if (Init() != -1)
            {
                GL.BindTexture(this.Target, this.ID);
            }
            return this;
        }

        public Texture Bind(TextureUnit unit)
        {
            GL.ActiveTexture(unit);
            this.Bind();
            return this;
        }

        public void Upload<T>(T[] data) where T : struct
        {
            if (Init() != -1)
            {
                this.Bind();
                this.ApplyParameters();
                this.UploadImage(data);
            }
        }

        public void Upload<T>(T[] data, int level) where T : struct
        {
            if (Init() != -1)
            {
                this.Bind();
                this.ApplyParameters();
                this.UploadImage(data,level);
            }
        }

        public void UploadEmpty()
        {
            if (Init() != -1)
            {
                this.Bind();
                this.ApplyParameters();
                GL.TexImage2D(this.Target, 0, this.InternalFormat, this.Width, this.Height, 0, this.Format, this.Type, IntPtr.Zero);
            }
        }

        public void UploadImage<T>(T[] data) where T : struct
        {
            log.Trace("Texture.UploadImage ({0}) uploading...", this.Name);
            GL.TexImage2D<T>(this.Target, 0, this.InternalFormat, this.Width, this.Height, 0, this.Format, this.Type, data);
            log.Trace("Texture.UploadImage ({0}) uploaded {1} texels of {2}", this.Name, data.Length, data.GetType().Name);
        }

        public void UploadImage<T>(T[] data, int level) where T : struct
        {
            log.Trace("Texture.UploadImage ({0}) uploading...", this.Name);
            GL.TexImage2D<T>(this.Target, level, this.InternalFormat, this.Width >> level, this.Height >> level, 0, this.Format, this.Type, data);
            log.Trace("Texture.UploadImage ({0}) uploaded {1} texels of {2}", this.Name, data.Length, data.GetType().Name);
        }

        public void RefreshImage<T>(T[] data) where T : struct
        {
            log.Trace("Texture.RefreshImage ({0}) uploading...", this.Name);
            this.Bind();
            GL.TexSubImage2D<T>(this.Target, 0, 0, 0, this.Width, this.Height, this.Format, this.Type, data);
            log.Trace("Texture.RefreshImage ({0}) uploaded {1} texels of {2}", this.Name, data.Length, data.GetType().Name);
        }

        public void RefreshImage<T>(T[] data, int level) where T : struct
        {
            log.Trace("Texture.RefreshImage ({0}) uploading...", this.Name);
            this.Bind();
            GL.TexSubImage2D<T>(this.Target, level, 0, 0, this.Width, this.Height, this.Format, this.Type, data);
            log.Trace("Texture.RefreshImage ({0}) uploaded {1} texels of {2}", this.Name, data.Length, data.GetType().Name);
        }

        public void RefreshImage<T>(T[] data, int xoffset, int yoffset, int width, int height) where T : struct
        {
            log.Trace("Texture.RefreshImage ({0}) uploading...", this.Name);
            this.Bind();
            GL.TexSubImage2D<T>(this.Target, 0, xoffset, yoffset, width, height, this.Format, this.Type, data);
            log.Trace("Texture.RefreshImage ({0}) uploaded {1} texels of {2}", this.Name, data.Length, data.GetType().Name);
        }

        public void RefreshImage(VBO buffer) 
        {
            log.Trace("Texture.RefreshImage ({0}) uploading from buffer...", this.Name);
            buffer.Bind();
            this.Bind();
            GL.TexSubImage2D(this.Target, 0, 0, 0, this.Width, this.Height, this.Format, this.Type, (IntPtr)IntPtr.Zero);
            //buffer.Unbind();
            log.Trace("Texture.RefreshImage ({0}) uploaded", this.Name);
        }

        public void Unload()
        {
            if (this.ID != -1)
            {
                GL.DeleteTexture(this.ID);
                this.ID = -1;
            }
        }

        public void Resize(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }
    }
}
