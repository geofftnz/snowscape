using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions.Components;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainGenerationViewer.UI.Debug
{
    public class QuadTreeLodDebugRenderer : GameComponentBase, IRenderable
    {
        private GameComponentCollection Components = new GameComponentCollection();
        private LineBuffer lineBuffer;

        public bool Visible { get; set; }
        public int DrawOrder { get; set; }

        public QuadTreeLodDebugRenderer()
        {
            this.Visible = true;
            this.DrawOrder = 0;

            this.Components.Add(lineBuffer = new LineBuffer(8192));
            this.Loading += QuadTreeLodDebugRenderer_Loading;

        }

        void QuadTreeLodDebugRenderer_Loading(object sender, EventArgs e)
        {
            this.Components.Load();

        }



        public void Render(IFrameRenderData frameData)
        {
            throw new NotImplementedException();
        }
    }
}
