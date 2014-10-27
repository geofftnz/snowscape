using System;
namespace Snowscape.TerrainRenderer.Renderers
{
    public interface IPatchRenderer : ITileRenderer
    {
        int Height { get; set; }
        void Load();
        OpenTK.Vector2 Offset { get; set; }
        new void Render(Snowscape.TerrainRenderer.TerrainTile tile, OpenTK.Matrix4 projection, OpenTK.Matrix4 view, OpenTK.Vector3 eyePos);
        float Scale { get; set; }
        float DetailScale { get; set; }
        void Unload();
        int Width { get; set; }
    }
}
