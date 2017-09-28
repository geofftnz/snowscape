using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using Utils;
using OpenTK.Graphics.OpenGL4;

namespace Snowscape.TerrainRenderer
{
    /// <summary>
    /// Creates a texture of noise for use in various algorithms in a shader
    /// </summary>
    public class NoiseTextureFactory
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public PixelInternalFormat TextureInternalFormat { get; set; }
        public PixelFormat TexturePixelFormat { get; set; }
        public PixelType TexturePixelType { get; set; }
        public TextureTarget Target { get; set; }

        public NoiseTextureFactory(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Target = TextureTarget.Texture2D;
            this.TextureInternalFormat = PixelInternalFormat.Rgba;
            this.TexturePixelFormat = PixelFormat.Rgba;
            this.TexturePixelType = PixelType.UnsignedByte;

            //this.NoiseTex = new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);
        }

        public NoiseTextureFactory()
            : this(256, 256)
        {

        }

        public Texture GenerateFloatTexture()
        {
            this.TextureInternalFormat = PixelInternalFormat.R32f;
            this.TexturePixelFormat = PixelFormat.Red;
            this.TexturePixelType = PixelType.Float;

            var t = new Texture(this.Width, this.Height, this.Target, this.TextureInternalFormat, this.TexturePixelFormat, this.TexturePixelType);

            t.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat));
            t.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));
            t.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear));
            t.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));

            // allocate space for texture
            var data = new float[this.Width * this.Height];

            var rand = new Random();
            float rx = (float)rand.NextDouble();
            float ry = (float)rand.NextDouble();

            // generate some noise
            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                data[i] = SimplexNoise.wrapfbm(
                    (float)x, (float)y,
                    (float)this.Width, (float)this.Height,
                    rx, ry,
                    10, // octaves
                    0.005f,  // scale
                    2.0f, // amplitude
                    h => Math.Abs(h),
                    h => h);

            });

            //data.Normalize();

            float xmin = data.Min();
            float xmax = data.Max();
            float scale = xmax - xmin;
            if (scale != 0.0f)
            {
                scale = 1.0f / scale;
            }
            else
            {
                scale = 1.0f;
            }

            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                data[i] = (data[i] - xmin) * scale;
            });

            t.Upload(data);

            return t;
        }

    }
}
