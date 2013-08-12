using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    /// <summary>
    /// Provides an easy way of generating textures, generally from noise.
    /// Everything done internally as floating point, but can be downconverted to ubyte.
    /// Single component only - 
    /// 
    /// Knows how to:
    /// - Apply various operations of noise to its data buffer
    /// 
    /// Knows about:
    /// - Its internal state
    /// </summary>
    public class TextureSynth
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        private float[] data;

        public TextureSynth(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.data = new float[this.Width * this.Height];
        }
        public TextureSynth()
            : this(256, 256)
        {
        }

        // output:
        public float[] GetData()
        {
            return this.data;
        }

        public byte[] GetBytes()
        {
            byte[] temp = new byte[this.Width * this.Height];

            ParallelHelper.For2DParallel(this.Width, this.Height, (i) =>
            {
                temp[i] = (byte)(data[i] * 255.0f);
            });

            return temp;
        }

        public void WriteComponent(byte[] dest, int component)
        {
            if (dest.Length < this.Width * this.Height * 4)
            {
                throw new InvalidOperationException("TextureSynth.WriteComponent - destination buffer too small");
            }

            ParallelHelper.For2DParallel(this.Width, this.Height, (i) =>
            {
                dest[i * 4 + component] = (byte)(data[i] * 255.0f);
            });

        }

        // operations:

        // clear
        public TextureSynth Clear(float value)
        {
            ParallelHelper.For2D(this.Width, this.Height, i => { data[i] = value; });
            return this;
        }
        public TextureSynth Clear()
        {
            return Clear(0f);
        }

        // normalise
        public TextureSynth Normalise()
        {
            // get min/max

            float min = data[0];
            float max = data[0];

            for (int i = 0; i < this.Width * this.Height; i++)
            {
                var f = data[i];
                if (f > max) max = f;
                if (f < min) min = f;
            }

            if (min < max)
            {
                var diff = max - min;

                ParallelHelper.For2D(this.Width, this.Height, i =>
                {
                    data[i] = (data[i] - min) / diff;
                });
            }

            return this;
        }


        public TextureSynth ForEach(Func<float, float> f)
        {
            if (f != null)
            {
                ParallelHelper.For2D(this.Width, this.Height, i => { data[i] = f(data[i]); });
            }
            return this;
        }
        public TextureSynth ForEach(Func<int, int, float, float> f)
        {
            if (f != null)
            {
                ParallelHelper.For2D(this.Width, this.Height, (x, y, i) => { data[i] = f(x, y, data[i]); });
            }
            return this;
        }

        public TextureSynth ApplyWrapNoise(int octaves, float scale, float amplitude, Func<float, float> transform, Func<float, float> postTransform, Func<float, float, float> applyOperation)
        {
            var rand = new Random();
            float rx = (float)rand.NextDouble();
            float ry = (float)rand.NextDouble();

            scale /= (float)this.Width;

            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                data[i] = applyOperation(data[i], SimplexNoise.wrapfbm(
                    (float)x, (float)y,
                    (float)this.Width, (float)this.Height,
                    rx, ry,
                    octaves,
                    scale,
                    amplitude,
                    transform,
                    postTransform));

            });
            return this;
        }

        public TextureSynth ApplyWrapNoise(int octaves, float scale, float amplitude, Func<float, float> transform, Func<float, float> postTransform)
        {
            return ApplyWrapNoise(octaves, scale, amplitude, transform, postTransform, (h, n) => n);
        }
        public TextureSynth AddWrapNoise(int octaves, float scale, float amplitude, Func<float, float> transform, Func<float, float> postTransform)
        {
            return ApplyWrapNoise(octaves, scale, amplitude, transform, postTransform, (h, n) => h + n);
        }
        public TextureSynth MulWrapNoise(int octaves, float scale, float amplitude, Func<float, float> transform, Func<float, float> postTransform)
        {
            return ApplyWrapNoise(octaves, scale, amplitude, transform, postTransform, (h, n) => h * n);
        }

        public TextureSynth ApplyWrapNoise(int octaves, float scale, float amplitude)
        {
            return ApplyWrapNoise(octaves, scale, amplitude, h => h, h => h);
        }


    }
}
