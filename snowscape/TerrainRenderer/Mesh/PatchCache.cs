using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snowscape.TerrainRenderer.Mesh
{
    public class PatchCache : Snowscape.TerrainRenderer.Mesh.IPatchCache
    {
        private Dictionary<int, TerrainPatchMesh> meshCache = new Dictionary<int, TerrainPatchMesh>();

        public PatchCache()
        {

        }

        public TerrainPatchMesh GetPatchMesh(int size)
        {
            if (!meshCache.ContainsKey(size))
            {
                var mesh = new TerrainPatchMesh(size,size);
                mesh.Load();
                meshCache.Add(size, mesh);
            }
            return meshCache[size];
        }
    }
}
