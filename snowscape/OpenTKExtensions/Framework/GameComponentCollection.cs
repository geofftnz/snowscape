using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Framework
{
    public class GameComponentCollection : List<IGameComponent>, ICollection<IGameComponent>
    {
        public GameComponentCollection()
        {

        }

        public void Load()
        {
            foreach (var component in this.OrderBy(c=>c.LoadOrder))
            {
                component.Load();
            }
        }

        public void Unload()
        {
            foreach (var component in this.OrderByDescending(c => c.LoadOrder))
            {
                component.Unload();
            }
        }

        public void Render<F>(F frameData) where F : IFrameRenderData
        {
            foreach (var component in this.Select(c => c as IRenderable).Where(c => c != null).Where(c => c.Visible).OrderBy(c => c.DrawOrder))
            {
                component.Render(frameData);
            }
        }

        public void Update<F>(F frameData) where F : IFrameUpdateData
        {
            foreach (var component in this.Select(c => c as IUpdateable).Where(c => c != null))
            {
                component.Update(frameData);
            }
        }

        public void Reload()
        {
            foreach (var component in this.Select(c => c as IReloadable).Where(c => c != null))
            {
                component.Reload();
            }
        }

        public void Add(IGameComponent component, int loadOrder)
        {
            component.LoadOrder = loadOrder;
            this.Add(component);
        }


    }
}
