using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace OpenTKExtensions.Text
{
    /// <summary>
    /// Block of text in the queue for rendering
    /// </summary>
    public class TextBlock
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public float Size { get; set; }
        public string Text { get; set; }
        public Vector4 Colour { get; set; }

        public TextBlock(string name, string text, Vector3 position, float size, Vector4 colour)
        {
            this.Name = name;
            this.Text = text;
            this.Position = position;
            this.Size = size;
            this.Colour = colour;
        }

    }
}
