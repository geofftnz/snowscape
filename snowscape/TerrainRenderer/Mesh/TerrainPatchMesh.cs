using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utils;

namespace Snowscape.TerrainRenderer.Mesh
{
    /// <summary>
    /// Represents a 2^n+1 patch of terrain. 
    /// 
    /// Parameters are supplied by textures, so the only things we need are the vertex, boxcoords (could theoretically come from the mesh vertex xz coords) and index VBOs.
    /// These meshes will be shared.
    /// </summary>
    public class TerrainPatchMesh
    {
        private VBO vertexVBO = new VBO("patch-vertex");
        private VBO boxcoordVBO = new VBO("patch-boxcoord");
        private VBO indexVBO = new VBO("patch-index", BufferTarget.ElementArrayBuffer);

        /// <summary>
        /// Width of data used to drive tile - actual width will be this + 1.
        /// </summary>
        public int Width { get; set; }
        public int Height { get; set; }
        public int VertexWidth { get { return this.Width + 1; } }
        public int VertexHeight { get { return this.Height + 1; } }

        public TerrainPatchMesh(int width, int height)
        {
            if (width < 1 || height < 1)
            {
                throw new InvalidOperationException("TerrainPatchMesh: Width and height must be at least 1");
            }
            this.Width = width;
            this.Height = height;
        }

        public void Load()
        {
            SetupMesh();
        }

        private void SetupMesh()
        {
            Vector3[] vertex = new Vector3[this.VertexWidth * this.VertexHeight];
            Vector3[] boxcoord = new Vector3[this.VertexWidth * this.VertexHeight];

            float xscale = 1.0f / (float)(this.Width);
            float zscale = 1.0f / (float)(this.Height);

            ParallelHelper.For2D(this.VertexWidth, this.VertexHeight, (x, z, i) =>
            {
                vertex[i].X = (float)x * xscale;
                vertex[i].Y = 0f;
                vertex[i].Z = (float)z * zscale;

                boxcoord[i].X = (float)x * xscale;
                boxcoord[i].Y = 0f;
                boxcoord[i].Z = (float)z * zscale;
            });

            // vertex VBO
            this.vertexVBO.SetData(vertex);
            // boxcoord VBO
            this.boxcoordVBO.SetData(boxcoord);

            // cubeindex VBO
            uint[] meshindex = new uint[this.Width * this.Height * 6];

            for (int y = 0; y < this.Height; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    int i = (x + y * (this.Width)) * 6;

                    if (y % 2 == 0)
                    {
                        meshindex[i + 0] = (uint)(x + y * this.VertexWidth);  // 0
                        meshindex[i + 1] = (uint)(x + 1 + y * this.VertexWidth);  // 1
                        meshindex[i + 2] = (uint)(x + (y + 1) * this.VertexWidth); // 2
                        meshindex[i + 3] = (uint)(x + 1 + y * this.VertexWidth); // 1
                        meshindex[i + 4] = (uint)(x + 1 + (y + 1) * this.VertexWidth); // 3
                        meshindex[i + 5] = (uint)(x + (y + 1) * this.VertexWidth); // 2
                    }
                    else
                    {
                        meshindex[i + 0] = (uint)(x + y * this.VertexWidth);  // 0
                        meshindex[i + 1] = (uint)(x + 1 + y * this.VertexWidth);  // 1
                        meshindex[i + 2] = (uint)(x + 1 + (y + 1) * this.VertexWidth); // 3 
                        meshindex[i + 3] = (uint)(x + y * this.VertexWidth);  // 0
                        meshindex[i + 4] = (uint)(x + 1 + (y + 1) * this.VertexWidth); // 3 
                        meshindex[i + 5] = (uint)(x + (y + 1) * this.VertexWidth); // 2
                    }
                }
            }

            indexVBO.SetData(meshindex);

        }

        public void Bind(int vertexLocation, int boxcoordLocation)
        {
            this.vertexVBO.Bind(vertexLocation);
            this.boxcoordVBO.Bind(boxcoordLocation);
            this.indexVBO.Bind();
        }

        public void Render()
        {
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);
        }

    }
}
