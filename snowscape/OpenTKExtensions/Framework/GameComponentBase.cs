using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;

namespace OpenTKExtensions.Framework
{
    public class GameComponentBase : IGameComponent
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

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


        public GameComponentBase()
        {
            this.Status = ComponentStatus.New;
        }

        /// <summary>
        /// Loads the component and all subcomponents
        /// </summary>
        public void Load()
        {
            log.Info("GameComponentBase.Load({0}) loading", this.GetType().Name);

            if (this.Status != ComponentStatus.New && this.Status != ComponentStatus.Unloaded)
            {
                log.Info("GameComponentBase.Load({0}) already loaded", this.GetType().Name);
                return;
                //throw new InvalidOperationException("Component was not in a valid state to load.");
            }

            this.Status = ComponentStatus.Loading;
            this.OnLoading(EventArgs.Empty);
            this.Status = ComponentStatus.Loaded;
            this.OnLoaded(EventArgs.Empty);

            log.Info("GameComponentBase.Load({0}) loaded", this.GetType().Name);
        }

        public void Unload()
        {
            log.Info("GameComponentBase.Unload({0}) unloading", this.GetType().Name);

            if (this.Status != ComponentStatus.Loaded)
            {
                log.Info("GameComponentBase.Unload({0}) already unloaded", this.GetType().Name);
                return;
                //throw new InvalidOperationException("Component was not in a valid state to unload.");
            }

            this.Status = ComponentStatus.Unloading;
            this.OnUnloading(EventArgs.Empty);
            this.Status = ComponentStatus.Unloaded;
            log.Info("GameComponentBase.Unload({0}) unloaded", this.GetType().Name);
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
