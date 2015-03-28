using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;
using OpenTKExtensions.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Snowscape.TerrainRenderer.Mesh
{

    /// <summary>
    /// experimental mesh with a constant LoD
    /// 
    /// Regular square mesh, but transformed in vertex shader.
    /// </summary>
    public class ConicalMesh : GameComponentBase, IGameComponent
    {
        private VBO vertexVBO = new VBO("patch-vertex");
        private VBO indexVBO = new VBO("patch-index", BufferTarget.ElementArrayBuffer);

        public int Width { get; set; }
        public int Height { get; set; }

        public ConicalMesh(int width, int height)
        {
            if (width < 1 || height < 1)
            {
                throw new InvalidOperationException("Conical: Width and height must be at least 1");
            }
            this.Width = width;
            this.Height = height;

            this.Loading += ConicalMesh_Loading;
        }

        void ConicalMesh_Loading(object sender, EventArgs e)
        {
            var meshgenerator = new OpenTKExtensions.Generators.GridMesh(Width, Height);

            this.vertexVBO.SetData(meshgenerator.VerticesXZ().ToArray());
            this.indexVBO.SetData(meshgenerator.Indices().ToArray());

        }

        public void Bind(int vertexLocation)
        {
            this.vertexVBO.Bind(vertexLocation);
            this.indexVBO.Bind();
        }

        public void Render()
        {
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);
        }



    }
}
