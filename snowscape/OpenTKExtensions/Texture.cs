using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace OpenTKExtensions
{
    public class Texture
    {
        public int ID { get; private set; }
        public TextureTarget Target { get; private set; }
        public PixelInternalFormat InternalFormat { get; private set; }
        public PixelFormat Format { get; set; }
        public PixelType Type { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public string Name { get; set; }

        public Texture(int width, int height,TextureTarget target,PixelInternalFormat internalFormat,PixelFormat format,PixelType type)
        {
            this.ID = 0;
            this.Target = target;
            this.InternalFormat = internalFormat;
            this.Width = width;
            this.Height = height;
            this.Format = format;
            this.Type = type;
        }

        public void GenerateID()
        {
            if (this.ID == 0)
            {
                this.ID = GL.GenTexture();
            }
        }

        public void Bind()
        {
            GL.BindTexture(this.Target, this.ID);
        }

        public void UploadImage(IntPtr data)
        {
            GL.TexImage2D(this.Target, 0, this.InternalFormat, this.Width, this.Height, 0, this.Format, this.Type, data);
        }

        public void UploadSubImage(IntPtr data)
        {
            GL.TexSubImage2D(this.Target, 0, 0, 0, this.Width, this.Height, this.Format, this.Type, data);
        }

        public void Unload()
        {
            if (this.ID != 0)
            {
                GL.DeleteTexture(this.ID);
            }
        }
    }
}
