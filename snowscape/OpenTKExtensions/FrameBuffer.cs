using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using NLog;

namespace OpenTKExtensions
{
    public class FrameBuffer
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public int ID { get; private set; }
        public FramebufferTarget Target { get; private set; }
        public string Name { get; private set; }
        public FramebufferErrorCode Status { get; private set; }

        public FrameBuffer(string name, FramebufferTarget target)
        {
            this.Name = name;
            this.Target = target;
            this.ID = -1;
            this.Status = FramebufferErrorCode.FramebufferUndefined;
        }
        public FrameBuffer(string name)
            : this(name, FramebufferTarget.Framebuffer)
        {
        }
        public FrameBuffer()
            : this("unnamed")
        {
        }

        public int Init()
        {
            if (this.ID == -1)
            {
                this.ID = GL.GenFramebuffer();
                log.Trace("FrameBuffer.GenFramebuffer ({0}) returned {1}", this.Name, this.ID);
            }
            return this.ID;
        }

        public void Bind()
        {
            Bind(this.Target);
        }
        public void Bind(FramebufferTarget target)
        {
            GL.BindFramebuffer(target, this.ID);
        }

        public void Unbind()
        {
            Unbind(this.Target);
        }
        public void Unbind(FramebufferTarget target)
        {
            GL.BindFramebuffer(target, 0);
        }

        public void Unload()
        {
            if (this.ID != -1)
            {
                GL.DeleteFramebuffer(this.ID);
                this.ID = -1;
            }
        }

        public FramebufferErrorCode GetStatus()
        {
            this.Status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            return this.Status;
        }

        public bool IsStatusOK()
        {
            return this.Status == FramebufferErrorCode.FramebufferComplete;
        }

        public void ClearColourBuffer(int drawBuffer, Vector4 colour)
        {
            float[] c = { colour.X, colour.Y, colour.Z, colour.W };
            GL.ClearBuffer(ClearBuffer.Color, drawBuffer, c);
        }

    }
}
