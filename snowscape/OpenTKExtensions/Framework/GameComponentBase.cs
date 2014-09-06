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

        private GameComponentManager components = new GameComponentManager();
        protected GameComponentManager Components
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
