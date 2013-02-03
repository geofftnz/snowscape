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
        public float TexcoordX { get; set; }
        public float TexcoordY { get; set; }
        public float TexcoordW { get; set; }
        public float TexcoordH { get; set; }
        public float XOffset { get; set; }
        public float YOffset { get; set; }
        public float XAdvance { get; set; }

        public float Width { get { return this.TexcoordW; } }
        public float Height { get { return this.TexcoordH; } }

        private Vector2[] Texcoord = new Vector2[4];

        public Vector2 TexTopLeft { get { return Texcoord[0]; } }
        public Vector2 TexTopRight { get { return Texcoord[1]; } }
        public Vector2 TexBottomLeft { get { return Texcoord[2]; } }
        public Vector2 TexBottomRight { get { return Texcoord[3]; } }

        public FontCharacter()
        {

        }

        public void NormalizeTexcoords(float width, float height)
        {
            if (width <= 0.0f || height <= 0.0f)
            {
                throw new ArgumentException("NormalizeTexcoords: width and height must be greater than zero.");
            }

            float x = this.TexcoordX / width;
            float y = this.TexcoordY / height;
            float w = this.TexcoordW / width;
            float h = this.TexcoordH / height;

            // top left
            Texcoord[0].X = x;
            Texcoord[0].Y = y;

            // top right
            Texcoord[1].X = x + w;
            Texcoord[1].Y = y;

            // bottom left
            Texcoord[2].X = x;
            Texcoord[2].Y = y + h;

            // bottom right
            Texcoord[3].X = x + w;
            Texcoord[3].Y = y + h;

        }

    }
}
