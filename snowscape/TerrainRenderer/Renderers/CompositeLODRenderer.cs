using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Snowscape.TerrainRenderer.Renderers
{

    /// <summary>
    /// Composite Tile Renderer - chooses different renderers based on distance to eye
    /// 
    /// Needs to apply the tile's model matrix to each corner and/or centre of the tile and take distance from that to the eye pos.
    /// Or could transform to screenspace for size visibility.
    /// 
    /// </summary>
    public class CompositeLODRenderer:ITileRenderer
    {
        private ITileRenderer distantRenderer;  // full tile, raycast.
        private ITileRenderer middleRenderer;  // 4x4 array of 256x256 tiles (or however many we need for full tile res)
        private ITileRenderer closeRenderer; // renderer with detail applied in the vertex shader

        public CompositeLODRenderer(ITileRenderer dist, ITileRenderer mid, ITileRenderer close)
        {
            this.distantRenderer = dist;
            this.middleRenderer = mid;
            this.closeRenderer = close;
        }

        public void Load()
        {
        }

        public void Render(TerrainTile tile, Matrix4 projection, Matrix4 view, Vector3 eye)
        {
            /*
            Vector3[] box = {
                                new Vector3(0f,0f,0f),
                                new Vector3(1f,0f,0f),
                                new Vector3(0f,0f,1f),
                                new Vector3(1f,0f,1f),
                                new Vector3(0f,1f,0f),
                                new Vector3(1f,1f,0f),
                                new Vector3(0f,1f,1f),
                                new Vector3(1f,1f,1f)
                            };

            // build box
            var projbox = box
                .Select(v => new Vector4(v.X * tile.Width, tile.MinHeight + v.Y * (tile.MaxHeight - tile.MinHeight), v.Z * tile.Height, 1.0f))
                .Select(v => Vector4.Transform(Vector4.Transform(Vector4.Transform(v, tile.ModelMatrix), view), projection))
                .Select(v=> new Vector2(v.X / v.W, v.Y / v.W))
                .ToArray();

            // width on screen
            var screenWidth = projbox.Select(v => v.X).Max() - projbox.Select(v => v.X).Min();
            */

            // compute distance from tile to eye
            var centre = Vector4.Transform(new Vector4(0.5f * (float)tile.Width, 0.0f, 0.5f * (float)tile.Height, 1.0f), tile.ModelMatrix);
            var distanceToCentre = (centre.Xz - eye.Xz).Length;

            if (distanceToCentre < 500.0f)  // todo: reference to pixels
            {
                distantRenderer.Render(tile, projection, view, eye);
                return;
            }
            middleRenderer.Render(tile, projection, view, eye);
            
        }

        public void Unload()
        {
        }
    }
}
