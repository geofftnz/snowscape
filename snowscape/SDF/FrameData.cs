using OpenTKExtensions.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDF
{
    public class FrameData : IFrameUpdateData, IFrameRenderData
    {
        public double Time
        {
            get;
            set;
        }
    }
}
