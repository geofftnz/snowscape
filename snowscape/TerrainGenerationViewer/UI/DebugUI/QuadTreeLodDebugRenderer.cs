using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTKExtensions;
using OpenTKExtensions.Components;
using OpenTKExtensions.Framework;
using Snowscape.TerrainRenderer.Renderers.LOD;

namespace Snowscape.TerrainGenerationViewer.UI.Debug
{
    public class QuadTreeLodDebugRenderer : GameComponentBase, IRenderable
    {

        public List<PatchDescriptor> tilePatches { get; set; }
        public Frustum viewFrustum { get; set; }
        public Matrix4 overlayModelview { get; set; }
        public Matrix4 overlayProjection { get; set; }

        private Matrix4 lineBufferModel = Matrix4.CreateScale(1.0f / (float)(1024 * 3)) *
                                          Matrix4.CreateTranslation(0.5f, 0.5f, 0.0f) *
                                          Matrix4.CreateScale(0.5f) *
                                          Matrix4.CreateScale(-1f, 1f, 1f) *
                                          Matrix4.CreateTranslation(1.0f, 0f, 0f);

        private GameComponentCollection Components = new GameComponentCollection();
        private LineBuffer lineBuffer;

        public bool Visible { get; set; }
        public int DrawOrder { get; set; }

        public QuadTreeLodDebugRenderer()
        {
            this.Visible = true;
            this.DrawOrder = 0;

            this.tilePatches = new List<PatchDescriptor>();

            this.Components.Add(lineBuffer = new LineBuffer(8192));
            this.Loading += QuadTreeLodDebugRenderer_Loading;
            this.Unloading += QuadTreeLodDebugRenderer_Unloading;

        }

        void QuadTreeLodDebugRenderer_Loading(object sender, EventArgs e)
        {
            this.Components.Load();

        }

        void QuadTreeLodDebugRenderer_Unloading(object sender, EventArgs e)
        {
            this.Components.Unload();
        }


        public void Render(IFrameRenderData _frameData)
        {
            this.lineBuffer.ClearLines();
            Vector4[] box = new Vector4[4];

            this.lineBuffer.SetColour(new Vector4(0f, 1f, 0f, 0.5f));

            foreach (var patch in tilePatches)
            {
                box[0] = new Vector4(0f, 0f, 0f, 1f);
                box[1] = new Vector4(1f, 0f, 0f, 1f);
                box[2] = new Vector4(0f, 0f, 1f, 1f);
                box[3] = new Vector4(1f, 0f, 1f, 1f);

                switch (patch.LOD)
                {
                    case -4: this.lineBuffer.SetColour(new Vector4(0.4f, 0f, 0f, 0.5f)); break;
                    case -3: this.lineBuffer.SetColour(new Vector4(0.6f, 0f, 0f, 0.5f)); break;
                    case -2: this.lineBuffer.SetColour(new Vector4(0.8f, 0f, 0f, 0.5f)); break;
                    case -1: this.lineBuffer.SetColour(new Vector4(1.0f, 0f, 0f, 0.5f)); break;
                    case 0: this.lineBuffer.SetColour(new Vector4(1.0f, 0.5f, 0f, 0.5f)); break;
                    case 1: this.lineBuffer.SetColour(new Vector4(1.0f, 0.8f, 0f, 0.5f)); break;
                    case 2: this.lineBuffer.SetColour(new Vector4(1.0f, 1.0f, 0f, 0.5f)); break;
                    case 3: this.lineBuffer.SetColour(new Vector4(0.5f, 1.0f, 0f, 0.5f)); break;
                    case 4: this.lineBuffer.SetColour(new Vector4(0.0f, 1.0f, 0f, 0.5f)); break;
                }

                for (int i = 0; i < 4; i++)
                {

                    box[i].X *= patch.Scale;
                    box[i].Z *= patch.Scale;

                    box[i].X += patch.Offset.X;
                    box[i].Z += patch.Offset.Y;

                    box[i].X *= (float)patch.Tile.Width;
                    box[i].Z *= (float)patch.Tile.Height;

                    box[i] = Vector4.Transform(box[i], patch.TileModelMatrix);
                }
                lineBuffer.MoveTo(box[0].TopDown());
                lineBuffer.LineTo(box[1].TopDown());
                lineBuffer.LineTo(box[3].TopDown());
                lineBuffer.LineTo(box[2].TopDown());
                lineBuffer.LineTo(box[0].TopDown());
            }

            DebugRenderFrustum(viewFrustum);

            this.lineBuffer.Render(lineBufferModel, overlayModelview, overlayProjection);
        }

        private void DebugRenderFrustum(Frustum f)
        {
            lineBuffer.SetColour(new Vector4(1f, 1f, 1f, 1f));
            lineBuffer.MoveTo(f.NearTopLeftCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.NearTopRightCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.FarTopRightCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.FarTopLeftCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.NearTopLeftCorner.Xyz.TopDown());

            lineBuffer.SetColour(new Vector4(1f, 1f, 1f, .5f));
            lineBuffer.MoveTo(f.NearBottomLeftCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.NearBottomRightCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.FarBottomRightCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.FarBottomLeftCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.NearBottomLeftCorner.Xyz.TopDown());

            lineBuffer.SetColour(new Vector4(1f, 0f, 0f, 1f));
            lineBuffer.MoveTo(f.NearTopLeftCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.FarTopLeftCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.FarBottomLeftCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.NearBottomLeftCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.NearTopLeftCorner.Xyz.TopDown());

            lineBuffer.SetColour(new Vector4(0f, 1f, 0f, 1f));
            lineBuffer.MoveTo(f.NearTopRightCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.FarTopRightCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.FarBottomRightCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.NearBottomRightCorner.Xyz.TopDown());
            lineBuffer.LineTo(f.NearTopRightCorner.Xyz.TopDown());
        }

    }
}
