using OpenTKExtensions.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerrainRenderer.Pipeline.Primary
{
    /// <summary>
    /// Primary Pipeline (crap name, but will do for now)
    /// 
    /// Encapsulates the main terrain rendering pipeline in one handy package.
    /// Renders out to the RGB16 HDR buffer.
    /// 
    /// Contains:
    /// - gbuffer with lighting parameters (might get factored out later)
    /// - terrain geometry rendering
    /// - sky-ray rendering
    /// - AO/shadow rendering (might get factored out)
    /// - sky precomputation (might get factored out)
    /// - copying of data from terrain generation
    /// 
    /// Also:
    /// - provides a parameter source
    /// - provides a debug texture source
    /// 
    /// Needs:
    /// - camera
    /// 
    /// </summary>
    public class PrimaryPipeline : GameComponentBase, IRenderable
    {
        public bool Visible { get; set; }
        public int DrawOrder { get; set; }

        private GameComponentCollection components = new GameComponentCollection();
        public GameComponentCollection Components { get { return components; } }

        public PrimaryPipeline()
        {



            this.Loading += PrimaryPipeline_Loading;
            this.Unloading += PrimaryPipeline_Unloading;
        }

        void PrimaryPipeline_Loading(object sender, EventArgs e)
        {
            this.Components.Load();
        }

        void PrimaryPipeline_Unloading(object sender, EventArgs e)
        {
            this.Components.Unload();
        }



        public void Render(IFrameRenderData frameData)
        {
            throw new NotImplementedException();
        }
    }
}
