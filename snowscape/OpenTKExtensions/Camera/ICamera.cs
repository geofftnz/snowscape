using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Input;
using OpenTKExtensions.Framework;

namespace OpenTKExtensions.Camera
{
    public interface ICamera : IUpdateable, IResizeable
    {
        //void Resize(int ClientWidth, int ClientHeight); 
        //void Update(double time);

        bool ViewEnable { get; set; }

        Matrix4 Projection { get; }
        Matrix4 View { get; }

        bool HasChanged();
        void ResetChanged();

    }
}
