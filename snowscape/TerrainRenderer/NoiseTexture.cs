using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using Utils;
using OpenTK.Graphics.OpenGL;

namespace Snowscape.TerrainRenderer
{
    /// <summary>
    /// Creates a texture of noise for use in various algorithms in a shader
    /// </summary>
    public class NoiseTexture
    {
        public Texture NoiseTex { get; private set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public NoiseTexture(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.NoiseTex = new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);
        }

        public void Init()
        {
            // init texture, bake in some noise


        }
    }
}
