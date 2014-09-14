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

        //private GameComponentCollection components = new GameComponentCollection();
        //protected GameComponentCollection Components
        //{
        //    get { return components; }
        //}





        public GameComponentBase()
        {
            this.Status = ComponentStatus.New;
        }

        /*
        public virtual void Load()
        {
            this.Components.Load();
        }

        public virtual void Unload()
        {
            this.Components.Unload();
        }*/

        /*
        public virtual void Update<F>(F frameData) where F : IFrameUpdateData
        {
            this.Components.Update(frameData);
        }

        public virtual void Render<F>(F frameData) where F : IFrameRenderData
        {
            this.Components.Render(frameData);
        }*/

        //protected void Add(IGameComponent subComponent)
        //{
        //    if (subComponent == null) throw new ArgumentNullException("subComponent");
        //    this.Components.Add(subComponent);
        //}

        //protected void Remove(IGameComponent subComponent)
        //{
        //    if (subComponent == null) throw new ArgumentNullException("subComponent");
        //    this.Components.Remove(subComponent);
        //}

        //protected void RemoveAllOf<T>()
        //{
        //    this.Components.RemoveAll(c => c is T);
        //}

        /*
        protected void LoadWrapper(Action loadAction)
        {
            if (this.Status != ComponentStatus.New && this.Status != ComponentStatus.Unloaded)
            {
                return;
            }

            this.Status = ComponentStatus.Loading;
            loadAction();
            this.Status = ComponentStatus.Loaded;
        }

        protected void UnloadWrapper(Action unloadAction)
        {
            if (this.Status != ComponentStatus.Loaded)
            {
                return;
            }

            this.Status = ComponentStatus.Unloading;
            unloadAction();
            this.Status = ComponentStatus.Unloaded;
        }*/


        /// <summary>
        /// Loads the component and all subcomponents
        /// </summary>
        public void Load()
        {
            if (this.Status != ComponentStatus.New && this.Status != ComponentStatus.Unloaded)
            {
                return;
                //throw new InvalidOperationException("Component was not in a valid state to load.");
            }

            this.Status = ComponentStatus.Loading;
            this.OnLoading(EventArgs.Empty);
            this.Status = ComponentStatus.Loaded;
            this.OnLoaded(EventArgs.Empty);
        }

        public void Unload()
        {
            if (this.Status != ComponentStatus.Loaded)
            {
                return;
                //throw new InvalidOperationException("Component was not in a valid state to unload.");
            }

            this.Status = ComponentStatus.Unloading;
            this.OnUnloading(EventArgs.Empty);
            this.Status = ComponentStatus.Unloaded;
        }


        /// <summary>
        /// Occurs when this component is being loaded.
        /// Derived classes should handle this event to load resources.
        /// </summary>
        public event EventHandler<EventArgs> Loading;


        public virtual void OnLoading(EventArgs e)
        {
            if (this.Loading != null)
            {
                this.Loading(this, e);
            }
        }

        /// <summary>
        /// Occurs when this component is being unloaded (all resources being released)
        /// Derived classes should handle this event
        /// </summary>
        public event EventHandler<EventArgs> Unloading;

        public virtual void OnUnloading(EventArgs e)
        {
            if (this.Unloading != null)
            {
                this.Unloading(this, e);
            }
        }

        /// <summary>
        /// Occurs after the component has been loaded.
        /// </summary>
        public event EventHandler<EventArgs> Loaded;

        public virtual void OnLoaded(EventArgs e)
        {
            if (this.Loaded != null)
            {
                this.Loaded(this, e);
            }
        }

    }
}
