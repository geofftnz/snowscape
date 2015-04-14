using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Framework
{
    public class CompositeGameComponent : GameComponentBase, IResizeable, IReloadable
    {
        protected GameComponentCollection components = new GameComponentCollection();
        public GameComponentCollection Components
        {
            get { return this.components; }
        }

        public CompositeGameComponent()
            : base()
        {
            this.Loading += CompositeGameComponent_Loading;
            this.Unloading += CompositeGameComponent_Unloading;
        }

        private void CompositeGameComponent_Loading(object sender, EventArgs e)
        {
            this.Components.Load();
        }

        private void CompositeGameComponent_Unloading(object sender, EventArgs e)
        {
            this.Components.Unload();
        }

        public virtual void Resize(int width, int height)
        {
            this.Components.Resize(width, height);
        }

        public virtual void Reload()
        {
            this.Components.Reload();
        }

    }
}
