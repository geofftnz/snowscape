using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Framework
{
    public interface IRenderable
    {
        bool Visible { get; set; }
        int DrawOrder { get; set; }
        void Render(IFrameRenderData frameData);
    }
}
