using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Framework
{
    public interface IGameComponent
    {
        // init the component, load any assets, init any graphics resources
        void Load();

        // release any resources created in Load()
        void Unload();

        /// <summary>
        /// Specifies the load order for the components.
        /// Components are unloaded in reverse order
        /// </summary>
        int LoadOrder { get; set; }

        // current state of component
        ComponentStatus Status { get; }
    }
}
