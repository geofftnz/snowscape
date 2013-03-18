using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using NLog;

namespace OpenTKExtensions
{
    public class GBuffer
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public class TextureSlotParam
        {
            public PixelInternalFormat InternalFormat { get; set; }
            public PixelFormat Format { get; set; }
            public PixelType Type { get; set; }
            public TextureSlotParam()
            {
            }
            public TextureSlotParam(PixelInternalFormat internalFormat, PixelFormat format, PixelType type)
            {
                this.InternalFormat = internalFormat;
                this.Format = format;
                this.Type = type;
            }
            public override string ToString()
            {
                return string.Format("[{0},{1},{2}]",InternalFormat.ToString(), Format.ToString(), Type.ToString());
            }
        }

        public class TextureSlot
        {
            public bool Enabled { get; set; }
            public int Slot { get; set; }
            public TextureSlotParam TextureParam { get; set; }
            public Texture Texture { get; set; }
            public int TextureID
            {
                get
                {
                    if (this.Texture != null)
                    {
                        return this.Texture.ID;
                    }
                    return -1;
                }
            }
            public DrawBuffersEnum DrawBufferSlot { get { return DrawBuffersEnum.ColorAttachment0 + this.Slot; } }
            public FramebufferAttachment FramebufferAttachmentSlot { get { return FramebufferAttachment.ColorAttachment0 + this.Slot; } }

            public TextureSlot()
            {
                this.Enabled = false;
                this.Slot = 0;
                this.TextureParam = new TextureSlotParam();
                this.Texture = null;
            }

            public void InitTexture(int Width, int Height)
            {
                this.Texture = new Texture(Width, Height, TextureTarget.Texture2D, this.TextureParam.InternalFormat, this.TextureParam.Format, this.TextureParam.Type);
                this.Texture
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge))
                    .UploadEmpty();
            }
            public void AttachToFramebuffer(FramebufferTarget target)
            {
                GL.FramebufferTexture2D(target, this.FramebufferAttachmentSlot, TextureTarget.Texture2D, this.TextureID, 0);
            }
        }

        const int MAXSLOTS = 16;

        private TextureSlot[] TextureSlots = new TextureSlot[MAXSLOTS];


        public Texture DepthTexture { get; private set; }
        public FrameBuffer FBO { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public string Name { get; private set; }

        public GBuffer(string name)
        {
            this.Name = name;
            this.FBO = new FrameBuffer(this.Name + "_GBuffer");

            for (int i = 0; i < MAXSLOTS; i++)
            {
                this.TextureSlots[i] = new TextureSlot();
            }
        }

        public GBuffer SetSlot(int slot, TextureSlotParam texparam)
        {
            if (slot < 0 || slot >= MAXSLOTS)
            {
                throw new InvalidOperationException("GBuffer.SetSlotTextureParams: slot out of range.");
            }

            this.TextureSlots[slot].Enabled = true;
            this.TextureSlots[slot].Slot = slot;
            this.TextureSlots[slot].TextureParam = texparam;

            log.Trace("GBuffer.SetSlot {0} = {1}", slot, texparam);

            return this;
        }

        private void InitAllTextures()
        {
            for (int i = 0; i < MAXSLOTS; i++)
            {
                if (this.TextureSlots[i].Enabled)
                {
                    this.TextureSlots[i].InitTexture(this.Width, this.Height);
                    this.TextureSlots[i].AttachToFramebuffer(this.FBO.Target);
                }
            }
        }

        private void SetDrawBuffers()
        {
            GL.DrawBuffers(this.TextureSlots.Where(s => s.Enabled).Count(), this.TextureSlots.Where(s => s.Enabled).Select(s => s.DrawBufferSlot).ToArray());
        }

        public bool Init(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            if (this.FBO.Init() != -1)
            {
                this.FBO.Bind();

                InitAllTextures();

                // dump old depth texture
                if (this.DepthTexture != null)
                {
                    this.DepthTexture.Unload();
                    this.DepthTexture = null;
                }
                // create & bind depth texture
                this.DepthTexture = new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.DepthComponent32f, PixelFormat.DepthComponent, PixelType.Float);
                this.DepthTexture.Init();
                this.DepthTexture.UploadEmpty();
                GL.FramebufferTexture2D(this.FBO.Target, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, this.DepthTexture.ID, 0);

                SetDrawBuffers();

                var status = this.FBO.GetStatus();
                log.Info("GBuffer.Init ({0}) FBO state is {1}", this.Name, this.FBO.Status.ToString());

                this.FBO.Unbind();

                return true;
            }
            return false;
        }

        public void BindForWriting()
        {
            this.FBO.Bind(FramebufferTarget.DrawFramebuffer);
            GL.Viewport(0, 0, this.Width, this.Height);
            SetDrawBuffers();
        }

        public void UnbindFromWriting()
        {
            this.FBO.Unbind(FramebufferTarget.DrawFramebuffer);
        }

        public void BindForReading()
        {
        }

        public Texture GetTextureAtSlot(int slot)
        {
            if (slot < 0 || slot >= MAXSLOTS)
            {
                throw new InvalidOperationException("GBuffer.SetSlotTextureParams: slot out of range.");
            }
            if (this.TextureSlots[slot] == null || !this.TextureSlots[slot].Enabled)
            {
                throw new InvalidOperationException(string.Format("GBuffer.SetSlotTextureParams: no texture at slot {0}",slot));
            }

            return this.TextureSlots[slot].Texture;

        }

    }
}
