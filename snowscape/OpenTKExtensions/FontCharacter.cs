using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;


namespace OpenTKExtensions
{
    public class FontCharacter
    {
        public int ID { get; set; }
        public float TexcoordS { get; set; }
        public float TexcoordT { get; set; }
        public float TexcoordW { get; set; }
        public float TexcoordH { get; set; }
        public float XOffset { get; set; }
        public float YOffset { get; set; }
        public float XAdvance { get; set; }

        public FontCharacter()
        {
                
        }
    }
}
