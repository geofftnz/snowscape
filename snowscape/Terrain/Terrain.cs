using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace Terrain
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
