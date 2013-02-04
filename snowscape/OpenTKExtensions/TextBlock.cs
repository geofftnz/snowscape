using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace OpenTKExtensions
{
    /// <summary>
    /// Block of text in the queue for rendering
    /// </summary>
    public class TextBlock
    {
        public Vector3 Position { get; set; }
        public float Size { get; set; }
        public string Text { get; set; }
    }
}
