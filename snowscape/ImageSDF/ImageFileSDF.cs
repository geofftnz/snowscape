using OpenTKExtensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageSDF
{
    public static class ImageFileSDF
    {

        public static void Generate(string sourceFileName, string destFileName)
        {
            ImageLoader.ImageInfo imageInfo;

            var imageData = ImageLoader.LoadImage(sourceFileName, out imageInfo).ExtractChannelFromRGBA(0);

            var generator = new SDFGenerator(imageInfo.Width, imageInfo.Height);

            var outputData = generator.Generate(imageData);

            // save image
            Bitmap b = new Bitmap(imageInfo.Width, imageInfo.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            //int i=0;
            //for (int y = 0; y < imageInfo.Height; y++)
            //{
            //    for (int x = 0; x < imageInfo.Width; x++)
            //    {
            //        b.SetPixel(x, y, Color.FromArgb(outputData[i], x & 0xff, y & 0xff));
            //        i++;
            //    }
            //}

            var data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, b.PixelFormat);
            unsafe
            {
                int i = 0;

                for (int y = 0; y < data.Height; y++)
                {
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);
                    int j = 0;

                    for (int x = 0; x < data.Width; x++)
                    {
                        row[j + 2] = outputData[i++];//R
                        row[j + 1] = 0;// (byte)(y & 0xff);//G
                        row[j + 0] = 0;// (byte)(x & 0xff);//B
                        row[j + 3] = 255;//A

                        j += 4;
                    }
                }

            }
            b.UnlockBits(data);
            

            b.Save(destFileName, ImageFormat.Png);

        }
    }
}
