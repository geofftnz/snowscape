using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions
{
    public interface IDrawableComponent
    {
        void Load();
        void Unload();
        void Update();
        void Render(RenderInfo renderInfo);
    }
}
