using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageSDF
{
    class Program
    {
        static void Main(string[] args)
        {
            ImageFileSDF.Generate("Resources/xero.png", "Resources/xerosdf.png");
        }
    }
}
