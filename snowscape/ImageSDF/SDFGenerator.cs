using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace ImageSDF
{
    public class SDFGenerator
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public struct DistanceSample
        {
            public int OffsetX;
            public int OffsetY;
            public float Distance;
        }

        private List<DistanceSample> SortedSamples;

        public SDFGenerator(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            SortedSamples = GenerateDistanceSamples(64).Where(d => d.Distance > 0.0f).OrderBy(d => d.Distance).ToList();
        }

        public byte[] Generate(byte[] input)
        {
            byte[] output = new byte[input.Length];

            ParallelHelper.For2DSingle(Width, Height, (x, y, i) =>
            {
                bool inside = input[i] > 127;
                if (!inside)
                    output[i] = (byte)SortedSamples.Where(d => input[Ofs(x, y, d)] > 127).Select(d => d.Distance).FirstOrDefault().ClampInclusive(0f, 255f);
            });

            return output;
        }

        public int Ofs(int x, int y, DistanceSample d)
        {
            return OfsComponent(x, d.OffsetX, this.Width) +
                    OfsComponent(y, d.OffsetY, this.Height) * this.Width;
        }
        public static int OfsComponent(int x, int ofs, int size)
        {
            int a = x + ofs;
            if (a < 0) return 0;
            if (a >= size) return size - 1;
            return a;
        }

        /// <summary>
        /// generates a square of supplied radius of samples
        /// </summary>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static IEnumerable<DistanceSample> GenerateDistanceSamples(int radius)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    yield return new DistanceSample { OffsetX = x, OffsetY = y, Distance = (float)Math.Sqrt((double)(x * x + y * y)) };
                }
            }
        }

    }
}
