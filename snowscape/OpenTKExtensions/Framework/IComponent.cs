using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Framework
{
    public interface IComponent
    {
        // init the component, load any assets, init any graphics resources
        void Load();

        // release any resources created in Load()
        void Unload();


        ComponentStatus Status { get; }
    }
}
