using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class ParallelHelper
    {
        const int BATCHSIZE = 4;

        public static void For2D(int Width, int Height, Action<int, int, int> op)
        {
            For2DParallel(Width, Height, op);
        }
        public static void For2D(int Width, int Height, Action<int> op)
        {
            For2DParallel(Width, Height, op);
        }
        public static void For2D(int Width, int Height, int YOffset, Action<int> op)
        {
            For2DParallelSubset(Width, Height, YOffset, op);
        }

        public static void For2DSingle(int Width, int Height, Action<int, int, int> op)
        {
            int i = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    op(x, y, i);
                    i++;
                }
            }
        }

        public static void For2DParallel(int Width, int Height, Action<int, int, int> op)
        {

            Parallel.For(0, Height, (y) =>
            {
                int i = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    op(x, y, i);
                    i++;
                }
            });
        }

        public static void For2DParallel(int Width, int Height, Action<int> op)
        {

            Parallel.For(0, Height, (y) =>
            {
                int i = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    op(i++);
                }
            });
        }

        public static void For2DParallelSubset(int Width, int Height, int yOffset, Action<int> op)
        {

            Parallel.For(yOffset, yOffset + Height, (y) =>
            {
                int i = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    op(i++);
                }
            });
        }


        public static void For2DParallelOffset(int Width, int Height, int xofs, int yofs, Action<int, int> op)
        {

            Parallel.For(0, Height, (y) =>
            {
                int i = y * Width;
                int y2 = ((y + yofs + Height) & (Height - 1));
                int x2 = (xofs + Width) & (Width - 1);
                int i2 = x2 + y2 * Width;
                op(i, i2);
                i++;

                i2 = 1 + xofs + y2 * Width;
                for (int x = 1; x < Width - 1; x++)
                {
                    op(i, i2);
                    i++; i2++;
                }

                i2 = ((Width - 1 + xofs) & (Width - 1)) + y2 * Width;
                op(i, i2);

            });
        }

        public static void For2DParallelUnrolled(int Width, int Height, Action<int, int, int> op)
        {
            if ((Width & 0x03) == 0)
            {
                Parallel.For(0, Height, (y) =>
                {
                    int i = y * Width;
                    for (int x = 0; x < Width; x += 4, i += 4)
                    {
                        op(x, y, i);
                        op(x + 1, y, i + 1);
                        op(x + 2, y, i + 2);
                        op(x + 3, y, i + 3);
                    }

                });
            }
            else
            {
                For2DParallel(Width, Height, op);
            }
        }
        public static void For2DParallelUnrolled(int Width, int Height, Action<int> op)
        {
            if ((Width & 0x03) == 0)
            {
                Parallel.For(0, Height, (y) =>
                {
                    int i = y * Width;
                    for (int x = 0; x < Width; x += 4, i += 4)
                    {
                        op(i);
                        op(i + 1);
                        op(i + 2);
                        op(i + 3);
                    }

                });
            }
            else
            {
                For2DParallel(Width, Height, op);
            }
        }

        public static void For2DParallelBatched(int Width, int Height, Action<int, int, int> op)
        {
            int hbatch = Height / BATCHSIZE;

            // run in BATCHSIZE row blocks
            Parallel.For(0, hbatch, (yy) =>
            {
                int i = yy * Width * BATCHSIZE;
                for (int y = yy; y < yy + BATCHSIZE; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        op(x, y, i);
                        i++;
                    }
                }
            });

            // run for the rest
            int ii = hbatch * Width * BATCHSIZE;
            for (int y = hbatch * BATCHSIZE; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    op(x, y, ii);
                    ii++;
                }
            }

        }

        public static void CopySingleThreadUnrolled<T>(T[] src, T[] dest, int count)
        {
            int i = 0;
            for (; i < count; i += 8)
            {
                dest[i] = src[i];
                dest[i + 1] = src[i + 1];
                dest[i + 2] = src[i + 2];
                dest[i + 3] = src[i + 3];
                dest[i + 4] = src[i + 4];
                dest[i + 5] = src[i + 5];
                dest[i + 6] = src[i + 6];
                dest[i + 7] = src[i + 7];
                //dest[i + 8] = src[i + 8];
                //dest[i + 9] = src[i + 9];
                //dest[i + 10] = src[i + 10];
                //dest[i + 11] = src[i + 11];
                //dest[i + 12] = src[i + 12];
                //dest[i + 13] = src[i + 13];
                //dest[i + 14] = src[i + 14];
                //dest[i + 15] = src[i + 15];
            }
            for (; i < count; i++)
            {
                dest[i] = src[i];
            }
        }

    }
}
