using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;
using System.Threading.Tasks;
using OpenTK;

namespace Snowscape.TerrainStorage
{
    /// <summary>
    /// Terrain 
    /// 
    /// knows about:
    /// - its grid size
    /// - the stack of material at each point on its grid
    /// 
    /// </summary>
    public class Terrain
    {

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Cell[] Map { get; private set; }

        public Func<int, int, int> C { get; private set; }
        public Func<int, int> CX { get; private set; }
        public Func<int, int> CY { get; private set; }

        public Cell this[int index]
        {
            get { return this.Map[index]; }
            set { this.Map[index] = value; }
        }

        public Terrain(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Map = new Cell[this.Width * this.Height];

            if (this.Width == 1024 && this.Height == 1024)
            {
                this.C = C1024;
                this.CX = CX1024;
                this.CY = CY1024;
            }
            else if (this.Width == 512 && this.Height == 512)
            {
                this.C = C512;
                this.CX = CX512;
                this.CY = CY512;
            }
            else if (this.Width == 256 && this.Height == 256)
            {
                this.C = C256;
                this.CX = CX256;
                this.CY = CY256;
            }
            else
            {
                this.C = (x, y) => x.Wrap(this.Width) + y.Wrap(this.Height) * this.Width;
                this.CX = (i) => i % this.Width;
                this.CY = (i) => i / this.Width;
            }

        }

        public static Terrain Clone(Terrain src)
        {
            var dest = new Terrain(src.Width, src.Height);
            dest.CopyFrom(src);
            return dest;
        }

        public void CopyFrom(Terrain src)
        {
            if (src == null)
            {
                throw new InvalidOperationException("Cannot copy from null terrain");
            }
            if (this.Width != src.Width || this.Height != src.Height)
            {
                throw new InvalidOperationException("Cannot copy terrains of different size");
            }

            ParallelHelper.CopySingleThreadUnrolled(src.Map, this.Map, this.Width * this.Height);
        }


        #region noise
        public void AddSimplexNoise(int octaves, float scale, float amplitude)
        {
            var r = new Random();

            float rx = (float)r.NextDouble();
            float ry = (float)r.NextDouble();


            Parallel.For(0, this.Height,
                (y) =>
                {
                    int i = (int)y * this.Width;
                    float s = (float)y / (float)this.Height;
                    for (int x = 0; x < this.Width; x++)
                    {
                        float h = 0.0f;
                        float t = (float)x / (float)this.Width;
                        for (int j = 1; j <= octaves; j++)
                        {
                            //h += SimplexNoise.noise((float)rx + x * scale * (1 << j), (float)ry + y * scale * (1 << j), j * 3.3f) * (amplitude / ((1 << j) + 1));
                            h += SimplexNoise.wrapnoise(s, t, (float)this.Width, (float)this.Height, rx, ry, (float)(scale * (1 << j))) * (float)(1.0 / ((1 << j) + 1));
                        }
                        this.Map[i].Hard += h * amplitude;
                        i++;
                    }
                }
            );
        }
        public void AddSimplexNoiseToLoose(int octaves, float scale, float amplitude)
        {
            var r = new Random();

            float rx = (float)r.NextDouble();
            float ry = (float)r.NextDouble();


            Parallel.For(0, this.Height,
                (y) =>
                {
                    int i = (int)y * this.Width;
                    float s = (float)y / (float)this.Height;
                    for (int x = 0; x < this.Width; x++)
                    {
                        float h = 0.0f;
                        float t = (float)x / (float)this.Width;
                        for (int j = 1; j <= octaves; j++)
                        {
                            //h += SimplexNoise.noise((float)rx + x * scale * (1 << j), (float)ry + y * scale * (1 << j), j * 3.3f) * (amplitude / ((1 << j) + 1));
                            h += SimplexNoise.wrapnoise(s, t, (float)this.Width, (float)this.Height, rx, ry, (float)(scale * (1 << j))) * (float)(1.0 / ((1 << j) + 1));
                        }
                        this.Map[i].Loose += h * amplitude;
                        i++;
                    }
                }
            );
        }

        public void AddSimplexNoise(int octaves, float scale, float amplitude, Func<float, float> transform, Func<float, float> postTransform)
        {
            var r = new Random();

            float rx = (float)r.NextDouble();
            float ry = (float)r.NextDouble();


            Parallel.For(0, this.Height,
                (y) =>
                {
                    int i = (int)y * this.Width;
                    float s = (float)y / (float)this.Height;
                    for (int x = 0; x < this.Width; x++)
                    {
                        float h = 0.0f;
                        float t = (float)x / (float)this.Width;
                        for (int j = 1; j <= octaves; j++)
                        {
                            h += transform(SimplexNoise.wrapnoise(s, t, (float)this.Width, (float)this.Height, rx, ry, (float)(scale * (1 << j))) * (float)(1.0 / ((1 << j) + 1)));
                        }

                        if (postTransform != null)
                        {
                            this.Map[i].Hard += postTransform(h) * amplitude;
                        }
                        else
                        {
                            this.Map[i].Hard += h * amplitude;
                        }
                        i++;
                    }
                }
            );
        }

        public void AddSimplexPowNoise(int octaves, float scale, float amplitude, float power, Func<float, float> postTransform)
        {
            var r = new Random();

            float rx = (float)r.NextDouble();
            float ry = (float)r.NextDouble();


            Parallel.For(0, this.Height,
                (y) =>
                {
                    int i = (int)y * this.Width;
                    float s = (float)y / (float)this.Height;
                    for (int x = 0; x < this.Width; x++)
                    {
                        float h = 0.0f;
                        float t = (float)x / (float)this.Width;
                        for (int j = 1; j <= octaves; j++)
                        {
                            //h += SimplexNoise.noise((float)rx + x * scale * (1 << j), (float)ry + y * scale * (1 << j), j * 3.3f) * (amplitude / ((1 << j) + 1));
                            h += SimplexNoise.wrapnoise(s, t, (float)this.Width, (float)this.Height, rx, ry, (float)(scale * (1 << j))) * (float)(1.0 / ((1 << j) + 1));
                        }
                        this.Map[i].Hard += postTransform((float)Math.Pow(h, power) * amplitude);
                        i++;
                    }
                }
            );
        }

        /// <summary>
        /// One set of noise multiplied by another.
        /// </summary>
        /// <param name="octaves"></param>
        /// <param name="scale"></param>
        /// <param name="amplitude"></param>
        /// <param name="transform"></param>
        /// <param name="postTransform"></param>
        public void AddMultipliedSimplexNoise(
            int octaves1, float scale1, Func<float, float> transform1, float offset1, float mul1,
            int octaves2, float scale2, Func<float, float> transform2, float offset2, float mul2,
            Func<float, float> postTransform,
            float amplitude)
        {
            var r = new Random();

            float rx = (float)r.NextDouble();
            float ry = (float)r.NextDouble();


            Parallel.For(0, this.Height,
                (y) =>
                {
                    int i = (int)y * this.Width;
                    float s = (float)y / (float)this.Height;
                    for (int x = 0; x < this.Width; x++)
                    {
                        float t = (float)x / (float)this.Width;

                        float h = 0.0f;

                        for (int j1 = 1; j1 <= octaves1; j1++)
                        {
                            h += transform1(SimplexNoise.wrapnoise(s, t, (float)this.Width, (float)this.Height, rx, ry, (float)(scale1 * (1 << j1))) * (float)(1.0 / ((1 << j1) + 1)));
                        }

                        h = h * mul1 + offset1;

                        float h2 = 0f;

                        for (int j2 = 1; j2 <= octaves2; j2++)
                        {
                            h2 += transform2(SimplexNoise.wrapnoise(s, t, (float)this.Width, (float)this.Height, rx, ry, (float)(scale2 * (1 << j2))) * (float)(1.0 / ((1 << j2) + 1)));
                        }

                        h2 = h2 * mul2 + offset2;

                        h *= h2;


                        if (postTransform != null)
                        {
                            this.Map[i].Hard += postTransform(h) * amplitude;
                        }
                        else
                        {
                            this.Map[i].Hard += h * amplitude;
                        }
                        i++;
                    }
                }
            );
        }


        public void AddDiscontinuousNoise(int octaves, float scale, float amplitude, float threshold)
        {
            var r = new Random(1);

            double rx = r.NextDouble();
            double ry = r.NextDouble();

            Parallel.For(0, this.Height,
                (y) =>
                {
                    int i = y * this.Width;
                    for (int x = 0; x < this.Width; x++)
                    {
                        //this.Map[i].Hard += SimplexNoise.noise((float)rx + x * scale * (1 << j), (float)ry + y * scale * (1 << j), j * 3.3f) * (amplitude / ((1 << j) + 1));

                        float a = 0.0f;
                        for (int j = 1; j < octaves; j++)
                        {
                            a += SimplexNoise.noise((float)rx + x * scale * (1 << j), (float)ry + y * scale * (1 << j), j * 3.3f) * (amplitude / ((1 << j) + 1));
                        }

                        if (a > threshold)
                        {
                            this.Map[i].Hard += amplitude;
                        }

                        i++;
                    }
                }
            );
        }
        #endregion

        public Vector3 GetNormalFromWHeight(int cx, int cy)
        {
            float h1 = Map[C(cx, cy - 1)].WHeight;
            float h2 = Map[C(cx, cy + 1)].WHeight;
            float h3 = Map[C(cx - 1, cy)].WHeight;
            float h4 = Map[C(cx + 1, cy)].WHeight;
            return Vector3.Normalize(new Vector3(h3 - h4, h1 - h2, 2f));
        }

        public Vector3 GetNormalFromHeight(int cx, int cy)
        {
            float h1 = Map[C(cx, cy - 1)].Height;
            float h2 = Map[C(cx, cy + 1)].Height;
            float h3 = Map[C(cx - 1, cy)].Height;
            float h4 = Map[C(cx + 1, cy)].Height;
            return Vector3.Normalize(new Vector3(h3 - h4, h1 - h2, 2f));
        }

        public void Clear(float height)
        {
            for (int i = 0; i < Width * Height; i++)
            {
                this.Map[i] = new Cell();
            }
        }

        public void SetBaseLevel()
        {
            float min = this.Map.Select(c => c.Hard).Min();

            for (int i = 0; i < Width * Height; i++)
            {
                this.Map[i].Hard -= min;
            }
        }

        public void AddLooseMaterial(float amount)
        {
            for (int i = 0; i < Width * Height; i++)
            {
                this.Map[i].Loose += amount;
            }
        }

        public void AddTempDiffMapToLoose(float[] TempDiffMap)
        {
            ParallelHelper.For2D(this.Width, this.Height, (i) => { this.Map[i].Loose += TempDiffMap[i]; });
        }

        public void DecayWater(float MovingWaterDecay, float WaterErosionDecay, float CarryingDecay)
        {
            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                this.Map[i].MovingWater *= MovingWaterDecay;
                this.Map[i].Erosion *= WaterErosionDecay;
                this.Map[i].Carrying *= CarryingDecay;
            });
        }

        public float HeightAt(float x, float y)
        {
            x = x.Wrap(this.Width);
            y = y.Wrap(this.Height);

            int xx = (int)x;
            int yy = (int)y;

            float xfrac = (x ) - (float)xx;
            float yfrac = (y ) - (float)yy;

            float h00 = this.Map[C(xx, yy)].Height;
            float h10 = this.Map[C(xx + 1, yy)].Height;
            float h01 = this.Map[C(xx, yy + 1)].Height;
            float h11 = this.Map[C(xx + 1, yy + 1)].Height;
            
            return yfrac.Lerp(xfrac.Lerp(h00, h10), xfrac.Lerp(h01, h11));
        }


        private static Func<int, int, int> C1024 = (x, y) => ((x + 1024) & 1023) + (((y + 1024) & 1023) << 10);
        private static Func<int, int> CX1024 = (i) => i & 1023;
        private static Func<int, int> CY1024 = (i) => (i >> 10) & 1023;

        private static Func<int, int, int> C512 = (x, y) => ((x + 512) & 511) + (((y + 512) & 511) << 9);
        private static Func<int, int> CX512 = (i) => i & 511;
        private static Func<int, int> CY512 = (i) => (i >> 9) & 511;

        private static Func<int, int, int> C256 = (x, y) => ((x + 256) & 255) + (((y + 256) & 255) << 8);
        private static Func<int, int> CX256 = (i) => i & 255;
        private static Func<int, int> CY256 = (i) => (i >> 8) & 255;
    }
}
