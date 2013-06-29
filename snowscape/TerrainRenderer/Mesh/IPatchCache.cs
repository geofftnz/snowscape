using System;
namespace Snowscape.TerrainRenderer.Mesh
{
    public interface IPatchCache
    {
        TerrainPatchMesh GetPatchMesh(int size);
    }
}
