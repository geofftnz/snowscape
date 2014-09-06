﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Framework
{
    public interface IUpdateable
    {
        void Update(IFrameUpdateData frameData);
    }
}
