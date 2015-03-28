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
            foreach (var component in this.OrderBy(c => c.LoadOrder))
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

        public void Do<T>(Action<T> action) where T : class
        {
            foreach (var component in this.SelectMany(c => (c as T).Enum()))
            {
                action(component);
            }
        }


        public void Render<F>(F frameData) where F : IFrameRenderData
        {
            foreach (var component in this.OfType<IRenderable>().Where(c => c.Visible).OrderBy(c => c.DrawOrder))
            {
                component.Render(frameData);
            }
        }

        public void Update<F>(F frameData) where F : IFrameUpdateData
        {
            this.Do<IUpdateable>(c => c.Update(frameData));
        }

        public void Reload()
        {
            this.Do<IReloadable>(c => c.Reload());
        }

        public void Resize(int width, int height)
        {
            this.Do<IResizeable>(c => c.Resize(width, height));
        }


        public void Add(IGameComponent component, int loadOrder)
        {
            component.LoadOrder = loadOrder;
            this.Add(component);
        }


    }
}
