using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace OpenTKExtensions
{
    public static class ImageLoader
    {

        public struct ImageInfo
        {
            public int Width;
            public int Height;
            public System.Drawing.Imaging.PixelFormat PixelFormat;
        }

        public static byte[] LoadImage(this string s)
        {
            ImageInfo info;
            return s.LoadImage(out info);
        }

        public static byte[] LoadImage(this string s, out ImageInfo info)
        {
            byte[] ret;
            var image = (Bitmap)Bitmap.FromFile(s);

            info = new ImageInfo();
            info.Width = image.Width;
            info.Height = image.Height;
            info.PixelFormat = image.PixelFormat;

            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, image.PixelFormat);

            unsafe
            {
                int i = 0;
                ret = new byte[data.Width * data.Height * 4];

                for (int y = 0; y < data.Height; y++)
                {
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);
                    int j = 0;

                    for (int x = 0; x < data.Width; x++)
                    {
                        ret[i++] = row[j + 2];//R
                        ret[i++] = row[j + 1];//G
                        ret[i++] = row[j + 0];//B
                        ret[i++] = row[j + 3];//A

                        j += 4;
                    }
                }

            }
            image.UnlockBits(data);

            return ret;
        }

        public static byte[] ExtractChannelFromRGBA(this byte[] input, int channelIndex)
        {
            if (channelIndex<0||channelIndex>3){
                throw new InvalidOperationException("channelIndex must be between 0 and 3");
            }

            var len = input.Length;

            if (channelIndex >= len)
            {
                throw new InvalidOperationException("channelIndex is outside of source data range");
            }

            var b = new byte[len / 4];
            int edi = 0;
            int esi = channelIndex;

            for (int i = 0; i < len / 4; i++)
            {
                b[edi] = input[esi];
                edi++;
                esi += 4;
            }

            return b;
        }

    }
}
