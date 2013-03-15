using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;

namespace Snowscape.TerrainRenderer
{

    /// <summary>
    /// A Terrain Tile...
    /// 
    /// knows about:
    /// - its bounding box (VBO)
    /// - its heightmap (texture)
    /// - its normalmap (texture)
    /// - its shading data (textures)
    /// 
    /// knows how to:
    /// - generate its bounding box from the supplied (full-scale) heightmap
    /// - generate its textures from a subset of the supplied full-scale textures
    /// </summary>
    public class TerrainTile
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public Texture HeightTexture { get; private set; }
        public Texture NormalTexture { get; private set; }
        public Texture ShadeTexture { get; private set; }

        public float MinHeight { get; private set; }
        public float MaxHeight { get; private set; }


        public TerrainTile(int width, int height)
        {
            this.Width = width;
            this.Height = height;    
        }



    }
}
