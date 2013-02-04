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

        public string Name { get; set; }

        private int handle = -1;
        public int Handle
        {
            get { return handle; }
            private set { handle = value; }
        }
        public bool Loaded { get; set; }
        public BufferTarget Target { get; set; }
        public BufferUsageHint UsageHint { get; set; }


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


        public VBO(string name, BufferTarget target, BufferUsageHint usageHint)
        {
            this.Name = name;
            this.Handle = -1;
            this.Loaded = false;
            this.Target = target;
            this.UsageHint = usageHint;
        }

        public VBO(string name, BufferTarget target)
            : this(name, target, BufferUsageHint.StaticDraw)
        {
        }

        public VBO(string name)
            : this(name, BufferTarget.ArrayBuffer)
        {
        }

        public int Init()
        {
            if (this.Handle < 0)
            {
                GL.GenBuffers(1, out handle);
                this.Handle = handle;

                log.Trace("VBO.Init ({0}): Handle is {1}", this.Name, this.Handle);
            }

            return this.Handle;
        }

        public void SetData<T>(T[] data, int elementSizeInBytes, VertexAttribPointerType pointerType, int fieldsPerElement) where T : struct
        {
            log.Trace("VBO.SetData ({0}): Loading...", this.Name);
            if (Init() != -1)
            {
                GL.BindBuffer(this.Target, this.Handle);

                this.stride = elementSizeInBytes;
                this.arraySize = data.Length * stride;
                this.pointerType = pointerType;
                this.fieldsPerElement = fieldsPerElement;

                GL.BufferData<T>(this.Target, new IntPtr(arraySize), data, this.UsageHint);
                this.Loaded = true;
                log.Trace("VBO.SetData ({0}): Loaded {1} elements, {2} bytes", this.Name, data.Length, arraySize);
            }
            else
            {
                log.Error("VBO.SetData ({0}): buffer not initialised", this.Name);
            }
        }


        public void SetData(Vector4[] data)
        {
            this.SetData(data, Vector4.SizeInBytes, VertexAttribPointerType.Float, 4);
        }
        public void SetData(Vector3[] data)
        {
            this.SetData(data, Vector3.SizeInBytes, VertexAttribPointerType.Float, 3);
        }
        public void SetData(Vector2[] data)
        {
            this.SetData(data, Vector2.SizeInBytes, VertexAttribPointerType.Float, 2);
        }
        public void SetData(uint[] data)
        {
            this.SetData(data, sizeof(uint), VertexAttribPointerType.UnsignedInt, 1);
        }

        public void Bind(int index)
        {
            if (this.Loaded)
            {
                GL.BindBuffer(this.Target, this.Handle);

                if (this.Target != BufferTarget.ElementArrayBuffer)
                {
                    GL.EnableVertexAttribArray(index);
                    GL.VertexAttribPointer(index, fieldsPerElement, pointerType, false, stride, 0);
                }
            }
            else
            {
                log.Warn("VBO.Bind ({0}): buffer not loaded", this.Name);
            }
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
