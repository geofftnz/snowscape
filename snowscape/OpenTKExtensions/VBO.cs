using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using NLog;

namespace OpenTKExtensions
{
    public class VBO
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private int handle = -1;
        public int Handle {
            get { return handle; }
            private set { handle = value; }
        }
        public bool Loaded { get; set; }
        public BufferTarget Target { get; set; }

        private int arraySize;
        private int stride;
        private VertexAttribPointerType pointerType;
        private int fieldsPerElement;

        public int Length
        {
            get
            {
                if (this.Loaded)
                {
                    return arraySize;
                }
                throw new InvalidOperationException("Length is not defined until the VBO has been filled with data");
            }
        }


        public VBO(BufferTarget target)
        {
            this.Handle = -1;
            this.Loaded = false;
            this.Target = target;
        }
        public VBO():this(BufferTarget.ArrayBuffer)
        {
                
        }

        public int Init()
        {
            if (this.Handle < 0)
            {
                GL.GenBuffers(1, out handle);

                log.Trace("GL.GenBuffers returned {0}",handle);
            }

            return handle;
        }

        public void SetData(Vector3[] data)
        {
            if (Init() != -1)
            {
                GL.BindBuffer(this.Target, this.Handle);

                stride = Vector3.SizeInBytes;
                arraySize = data.Length * stride;
                pointerType = VertexAttribPointerType.Float;
                fieldsPerElement = 3;

                GL.BufferData<Vector3>(this.Target, new IntPtr(arraySize), data, BufferUsageHint.StaticDraw);
                this.Loaded = true;
            }
            else
            {
                log.Error("VBO.SetData - buffer not initialised");
            }
        }

        public void SetData(uint[] data)
        {
            if (Init() != -1)
            {
                GL.BindBuffer(this.Target, this.Handle);

                stride = sizeof(uint);
                arraySize = data.Length * stride;
                pointerType = VertexAttribPointerType.UnsignedInt;
                fieldsPerElement = 1;

                GL.BufferData<uint>(this.Target, new IntPtr(arraySize), data, BufferUsageHint.StaticDraw);
                this.Loaded = true;
            }
            else
            {
                log.Error("VBO.SetData - buffer not initialised");
            }
        }

        public void Bind(int index, int shaderProgramHandle, string shaderInputName)
        {
            if (this.Loaded)
            {
                GL.BindBuffer(this.Target, this.Handle);

                if (this.Target != BufferTarget.ElementArrayBuffer)
                {
                    GL.EnableVertexAttribArray(index);
                    GL.BindAttribLocation(shaderProgramHandle, index, shaderInputName);
                    GL.VertexAttribPointer(0, fieldsPerElement, pointerType, false, stride, 0);
                }
            }
            else
            {
                log.Warn("VBO.Bind - buffer not loaded");
            }
        }

        public void Bind(int index, ShaderProgram program, string shaderInputName)
        {
            Bind(index, program.Handle, shaderInputName);
        }

        public void Bind()
        {
            if (this.Loaded)
            {
                GL.BindBuffer(this.Target, this.Handle);
            }
        }


    }
}
