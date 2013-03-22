using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Input;

namespace OpenTKExtensions.Camera
{
    public interface ICamera
    {
        void Resize(int ClientWidth, int ClientHeight); 
        void Update(double time);

        Matrix4 Projection { get; }
        Matrix4 View { get; }

    }
}
