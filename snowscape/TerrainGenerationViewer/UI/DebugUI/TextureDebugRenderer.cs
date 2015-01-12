using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainGenerationViewer.UI.DebugUI
{
    public class TextureDebugRenderer : GameComponentBase, IRenderable
    {

        public enum RenderMode
        {
            RGB = 0,
            R,
            G,
            B,
            A,
            /// <summary>
            /// RGB, normalized around 0,0,0
            /// </summary>
            Normal,

            /// <summary>
            /// DO NOT USE - used to mark the end of the enum.
            /// </summary>
            MAX
        }

        public class TextureInfo
        {
            public Texture Texture { get; set; }
            public RenderMode RenderMode { get; set; }
            public TextureInfo()
            {
            }
        }

        private List<TextureInfo> textures = new List<TextureInfo>();
        public List<TextureInfo> Textures
        {
            get { return textures; }
        }

        public TextureDebugRenderer()
        {
            Visible = true;
            DrawOrder = 0;

            this.Loading += TextureDebugRenderer_Loading;
        }

        void TextureDebugRenderer_Loading(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Add(Texture texture, RenderMode renderMode = RenderMode.RGB)
        {
            if (!textures.Any(ti => ti.Texture == texture))
            {
                textures.Add(new TextureInfo { Texture = texture, RenderMode = renderMode });
            }
        }

        public bool Visible { get; set; }
        public int DrawOrder { get; set; }

        public void Render(IFrameRenderData frameData)
        {
            
        }
    }
}




