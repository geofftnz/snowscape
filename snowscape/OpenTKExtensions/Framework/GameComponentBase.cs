using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Framework
{
    public class GameComponentBase : IGameComponent
    {
        public ComponentStatus Status
        {
            get;
            protected set;
        }

        public int LoadOrder
        {
            get;
            set;
        }

        private GameComponentCollection components = new GameComponentCollection();
        protected GameComponentCollection Components
        {
            get { return components; }
        }

        public GameComponentBase()
        {
            this.Status = ComponentStatus.New;
        }

        public virtual void Load()
        {
            this.Components.Load();
        }

        public virtual void Unload()
        {
            this.Components.Unload();
        }

        public virtual void Update<F>(F frameData) where F : IFrameUpdateData
        {
            this.Components.Update(frameData);
        }

        public virtual void Render<F>(F frameData) where F : IFrameRenderData
        {
            this.Components.Render(frameData);
        }

        protected void Add(IGameComponent subComponent)
        {
            if (subComponent == null) throw new ArgumentNullException("subComponent");
            this.Components.Add(subComponent);
        }

        protected void Remove(IGameComponent subComponent)
        {
            if (subComponent == null) throw new ArgumentNullException("subComponent");
            this.Components.Remove(subComponent);
        }

        protected void RemoveAllOf<T>()
        {
            this.Components.RemoveAll(c => c is T);
        }

    }
}
