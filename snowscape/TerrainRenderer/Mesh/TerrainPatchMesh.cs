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

        private int VertexCount
        {
            get
            {
                return
                    this.VertexWidth * this.VertexHeight + // core mesh
                    this.VertexWidth * 2 + // top & bottom edges
                    this.VertexHeight * 2; // left and right edges
            }
        }

        private int TriangleCount
        {
            get
            {
                return
                    this.Width * this.Height * 2 + // core mesh
                    this.Width * 4 + // top and bottom edges
                    this.Height * 4; // left and right edges
            }
        }

        private int IndexCount
        {
            get
            {
                return TriangleCount * 3;
            }
        }

        private enum Edge
        {
            Top = 0,
            Bottom,
            Left,
            Right
        }

        private int GetSkirtVertexStart(Edge e)
        {
            int coreMeshEnd = this.VertexWidth * this.VertexHeight;

            switch (e)
            {
                case Edge.Top: return coreMeshEnd;
                case Edge.Bottom: return coreMeshEnd + this.VertexWidth;
                case Edge.Left: return coreMeshEnd + this.VertexWidth * 2;
                case Edge.Right:
                default: return coreMeshEnd + this.VertexWidth * 2 + this.VertexHeight;
            }
        }

        private void SetupMesh()
        {
            Vector3[] vertex = new Vector3[this.VertexCount];
            Vector3[] boxcoord = new Vector3[this.VertexCount];

            float xscale = 1.0f / (float)(this.Width);
            float zscale = 1.0f / (float)(this.Height);

            // setup core mesh
            ParallelHelper.For2D(this.VertexWidth, this.VertexHeight, (x, z, i) =>
            {
                vertex[i].X = (float)x * xscale;
                vertex[i].Y = 0f;
                vertex[i].Z = (float)z * zscale;

                boxcoord[i].X = (float)x * xscale;
                boxcoord[i].Y = 0f;
                boxcoord[i].Z = (float)z * zscale;
            });

            // setup edges as copies of core edges
            int j = this.VertexWidth * this.VertexHeight; // start vertex

            for (int i = 0; i < this.VertexWidth; i++)
            {
                vertex[j] = vertex[i]; // top edge
                boxcoord[j] = vertex[j];
                vertex[j].Y = -1.0f;
                j++;
            }
            for (int i = 0; i < this.VertexWidth; i++)
            {
                vertex[j] = vertex[this.Height * this.VertexWidth + i]; // bottom edge
                boxcoord[j] = vertex[j];
                vertex[j].Y = -1.0f;
                j++;
            }
            for (int i = 0; i < this.VertexHeight; i++)
            {
                vertex[j] = vertex[i * this.VertexWidth]; // left edge
                boxcoord[j] = vertex[j];
                vertex[j].Y = -1.0f;
                j++;
            }
            for (int i = 0; i < this.VertexHeight; i++)
            {
                vertex[j] = vertex[i * this.VertexWidth + this.Width]; // right edge
                boxcoord[j] = vertex[j];
                vertex[j].Y = -1.0f;
                j++;
            }



            // vertex VBO
            this.vertexVBO.SetData(vertex);
            // boxcoord VBO
            this.boxcoordVBO.SetData(boxcoord);

            // cubeindex VBO
            uint[] meshindex = new uint[this.IndexCount];

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

            // setup edge indices
            int ii = this.Width * this.Height * 6; // index start point

            // top edge
            for (int x = 0; x < this.Width; x++)
            {
                // work out indices of each corner 
                // 0 1  core mesh
                // 2 3  skirt

                uint index0 = (uint)(x);
                uint index1 = (uint)(x + 1);
                uint index2 = (uint)(GetSkirtVertexStart(Edge.Top) + x);
                uint index3 = (uint)(index2 + 1);

                meshindex[ii + 0] = index0;  // 0
                meshindex[ii + 1] = index2; // 2
                meshindex[ii + 2] = index1;  // 1
                meshindex[ii + 3] = index1; // 1
                meshindex[ii + 4] = index2; // 2
                meshindex[ii + 5] = index3; // 3

                ii += 6;
            }
            // bottom edge
            for (int x = 0; x < this.Width; x++)
            {
                // work out indices of each corner 
                // 0 1  core mesh
                // 2 3  skirt

                uint index0 = (uint)(x + this.Height * this.VertexWidth);
                uint index1 = (uint)(x + this.Height * this.VertexWidth + 1);
                uint index2 = (uint)(GetSkirtVertexStart(Edge.Bottom) + x);
                uint index3 = (uint)(index2 + 1);

                meshindex[ii + 0] = index0;  // 0
                meshindex[ii + 1] = index1;  // 1
                meshindex[ii + 2] = index2; // 2
                meshindex[ii + 3] = index1; // 1
                meshindex[ii + 4] = index3; // 3
                meshindex[ii + 5] = index2; // 2

                ii += 6;
            }
            // left edge
            for (int y = 0; y < this.Height; y++)
            {
                // work out indices of each corner 
                // 0 1  core mesh
                // 2 3  skirt

                uint index0 = (uint)(y * this.VertexWidth);
                uint index1 = (uint)((y + 1) * this.VertexWidth);
                uint index2 = (uint)(GetSkirtVertexStart(Edge.Left) + y);
                uint index3 = (uint)(index2 + 1);

                meshindex[ii + 0] = index0;  // 0
                meshindex[ii + 1] = index1;  // 1
                meshindex[ii + 2] = index2; // 2
                meshindex[ii + 3] = index1; // 1
                meshindex[ii + 4] = index3; // 3
                meshindex[ii + 5] = index2; // 2

                ii += 6;
            }
            // right edge
            for (int y = 0; y < this.Height; y++)
            {
                // work out indices of each corner 
                // 0 1  core mesh
                // 2 3  skirt

                uint index0 = (uint)(y * this.VertexWidth + this.Width);
                uint index1 = (uint)((y + 1) * this.VertexWidth + this.Width);
                uint index2 = (uint)(GetSkirtVertexStart(Edge.Right) + y);
                uint index3 = (uint)(index2 + 1);

                meshindex[ii + 0] = index0;  // 0
                meshindex[ii + 1] = index2; // 2
                meshindex[ii + 2] = index1;  // 1
                meshindex[ii + 3] = index1; // 1
                meshindex[ii + 4] = index2; // 2
                meshindex[ii + 5] = index3; // 3

                ii += 6;
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
