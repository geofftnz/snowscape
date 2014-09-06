﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Framework
{
    public class GameComponentManager : List<IGameComponent>, ICollection<IGameComponent>
    {
        public GameComponentManager()
        {

        }

        public void Load()
        {
            foreach (var component in this)
            {
                component.Load();
            }
        }

        public void Unload()
        {
            foreach (var component in this)
            {
                component.Unload();
            }
        }

        public void Render<F>(F frameData) where F : IFrameRenderData
        {
            foreach (var component in this.Select(c => c as IRenderable).Where(c => c != null).Where(c => c.Visible).OrderBy(c => c.DrawOrder))
            {
                component.Render(frameData);
            }
        }

        public void Update<F>(F frameData) where F : IFrameUpdateData
        {
            foreach (var component in this.Select(c => c as IUpdateable).Where(c => c != null))
            {
                component.Update(frameData);
            }
        }


    }
}
