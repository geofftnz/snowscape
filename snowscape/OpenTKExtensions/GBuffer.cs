using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using NLog;
using OpenTK;

namespace OpenTKExtensions
{
    public class GBuffer
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public class TextureSlotParam
        {
            public TextureTarget Target { get; set; }
            public PixelInternalFormat InternalFormat { get; set; }
            public PixelFormat Format { get; set; }
            public PixelType Type { get; set; }
            private List<ITextureParameter> textureParameters = new List<ITextureParameter>();
            public List<ITextureParameter> TextureParameters { get { return textureParameters; } }
            public bool MipMaps { get; set; }

            public TextureSlotParam()
                : this(TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte, false, GetDefaultTextureParameters())
            {
            }
            public TextureSlotParam(TextureTarget target, PixelInternalFormat internalFormat, PixelFormat format, PixelType type, bool mipmaps, IEnumerable<ITextureParameter> texParams)
            {
                this.Target = target;
                this.InternalFormat = internalFormat;
                this.Format = format;
                this.Type = type;
                this.MipMaps = mipmaps;
                this.TextureParameters.AddRange(texParams);
            }

            public TextureSlotParam(PixelInternalFormat internalFormat, PixelFormat format, PixelType type)
                : this(TextureTarget.Texture2D, internalFormat, format, type, false, GetDefaultTextureParameters())
            {
            }

            public override string ToString()
            {
                return string.Format("[{0},{1},{2}]", InternalFormat.ToString(), Format.ToString(), Type.ToString());
            }

            public void ApplyParametersTo(Texture t)
            {
                foreach (var tp in this.TextureParameters)
                {
                    t.SetParameter(tp);
                }
            }

            private static IEnumerable<ITextureParameter> GetDefaultTextureParameters()
            {
                yield return new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                yield return new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                yield return new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                yield return new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            }

        }

        public class TextureSlot
        {

            /// <summary>
            /// Slot has texture defined
            /// </summary>
            public bool Enabled { get; set; }

            /// <summary>
            /// Texture is externally defined (does not need to be managed by GBuffer)
            /// </summary>
            public bool External { get; set; }
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
                this.External = false;
                this.Slot = 0;
                this.TextureParam = new TextureSlotParam();
                this.Texture = null;
            }

            public TextureSlot(int colourAttachmentSlot, Texture texture)
            {
                this.Enabled = true;
                this.External = true;
                this.Slot = colourAttachmentSlot;
                this.TextureParam = new TextureSlotParam();
                this.Texture = texture;
            }

            public TextureSlot(int colourAttachmentSlot, Texture texture, TextureTarget target)
            {
                this.Enabled = true;
                this.External = true;
                this.Slot = colourAttachmentSlot;
                this.TextureParam = new TextureSlotParam() { Target = target };
                this.Texture = texture;
            }

            public void InitTexture(int Width, int Height)
            {
                if (this.Texture == null && !this.External)
                {
                    this.Texture = new Texture(Width, Height, TextureTarget.Texture2D, this.TextureParam.InternalFormat, this.TextureParam.Format, this.TextureParam.Type);
                }

                if (!this.External)
                {
                    this.TextureParam.ApplyParametersTo(this.Texture);
                    this.Texture.UploadEmpty();
                    if (this.TextureParam.MipMaps)
                    {
                        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                    }
                }
            }
            public void UnloadTexture()
            {
                if (this.Enabled && !this.External && this.Texture != null)
                {
                    this.Texture.Unload();
                    this.Texture = null;
                }
            }
            public void AttachToFramebuffer(FramebufferTarget target)
            {
                GL.FramebufferTexture2D(target, this.FramebufferAttachmentSlot, this.TextureParam.Target, this.TextureID, 0);
            }
        }

        public const int MAXSLOTS = 16;

        private TextureSlot[] TextureSlots = new TextureSlot[MAXSLOTS];


        public Texture DepthTexture { get; private set; }
        public FrameBuffer FBO { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public string Name { get; private set; }
        public bool WantDepth { get; set; }

        public GBuffer(string name, bool wantDepth)
        {
            this.Name = name;
            this.WantDepth = wantDepth;
            this.FBO = new FrameBuffer(this.Name + "_GBuffer");

            for (int i = 0; i < MAXSLOTS; i++)
            {
                this.TextureSlots[i] = new TextureSlot();
            }
        }

        public GBuffer(string name)
            : this(name, true)
        {

        }
        public GBuffer()
            : this("unnamed")
        {

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

        public GBuffer SetSlot(int slot, Texture texture)
        {
            if (slot < 0 || slot >= MAXSLOTS)
            {
                throw new InvalidOperationException("GBuffer.SetSlotTextureParams: slot out of range.");
            }

            this.TextureSlots[slot].Enabled = true;
            this.TextureSlots[slot].External = true;
            this.TextureSlots[slot].Slot = slot;
            this.TextureSlots[slot].Texture = texture;
            this.TextureSlots[slot].TextureParam = new TextureSlotParam(texture.InternalFormat, texture.Format, texture.Type);


            log.Trace("GBuffer.SetSlot {0} = {1}", slot, this.TextureSlots[slot].TextureParam);

            return this;
        }


        private void InitAllTextures()
        {
            for (int i = 0; i < MAXSLOTS; i++)
            {
                if (this.TextureSlots[i].Enabled)
                {
                    if (!this.TextureSlots[i].External)
                    {
                        this.TextureSlots[i].InitTexture(this.Width, this.Height);
                    }
                    this.TextureSlots[i].AttachToFramebuffer(this.FBO.Target);
                }
            }
        }

        private void UnloadAndDestroyAllTextures()
        {
            for (int i = 0; i < MAXSLOTS; i++)
            {
                if (this.TextureSlots[i].Enabled && !this.TextureSlots[i].External)
                {
                    this.TextureSlots[i].UnloadTexture();
                }
            }
        }

        private void SetDrawBuffers()
        {
            if (this.TextureSlots.Any(s => s.Enabled))
            {
                GL.DrawBuffers(this.TextureSlots.Where(s => s.Enabled).Count(), this.TextureSlots.Where(s => s.Enabled).Select(s => s.DrawBufferSlot).ToArray());
            }
        }

        public bool Init(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            log.Info("GBuffer.Init ({0}) creating G-Buffer of size {1}x{2}", this.Name, this.Width, this.Height);

            if (this.FBO.Init() != -1)
            {
                this.FBO.Bind();

                UnloadAndDestroyAllTextures();
                InitAllTextures();

                if (this.WantDepth)
                {
                    InitAndAttachDepthTexture();
                }

                SetDrawBuffers();

                var status = this.FBO.GetStatus();
                log.Info("GBuffer.Init ({0}) FBO state is {1}", this.Name, this.FBO.Status.ToString());

                this.FBO.Unbind();

                return true;
            }
            return false;
        }

        private void InitAndAttachDepthTexture()
        {
            // dump old depth texture
            if (this.DepthTexture != null)
            {
                this.DepthTexture.Unload();
                this.DepthTexture = null;
            }
            // create & bind depth texture
            this.DepthTexture = new Texture(this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.DepthComponent32, PixelFormat.DepthComponent, PixelType.Float);
            this.DepthTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp));
            this.DepthTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp));
            this.DepthTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest));
            this.DepthTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest));
            this.DepthTexture.SetParameter(new TextureParameterInt(TextureParameterName.DepthTextureMode, (int)All.Intensity));
            this.DepthTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureCompareMode, (int)TextureCompareMode.None));
            this.DepthTexture.SetParameter(new TextureParameterInt(TextureParameterName.TextureCompareFunc, (int)All.None));
            this.DepthTexture.Init();
            this.DepthTexture.UploadEmpty();
            this.DepthTexture.Bind();
            GL.FramebufferTexture2D(this.FBO.Target, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, this.DepthTexture.ID, 0);
        }

        public void BindForWriting()
        {
            this.FBO.Bind(FramebufferTarget.DrawFramebuffer);
            GL.Viewport(0, 0, this.Width, this.Height);
            SetDrawBuffers();
            if (this.WantDepth)
            {
                GL.Enable(EnableCap.DepthTest);
                GL.DepthMask(true);
            }

        }

        /// <summary>
        /// Bind GBuffer for writing to the supplied textures
        /// </summary>
        /// <param name="outputTextures"></param>
        public void BindForWritingTo(params TextureSlot[] outputTextures)
        {
            // shouldn't call this if we've got any texture slots defined.

            this.FBO.Bind(FramebufferTarget.DrawFramebuffer);
            GL.Viewport(0, 0, this.Width, this.Height);

            for (int i = 0; i < outputTextures.Length; i++)
            {
                outputTextures[i].AttachToFramebuffer(this.FBO.Target);
            }

            GL.DrawBuffers(outputTextures.Length, outputTextures.Select(t => t.DrawBufferSlot).ToArray());
        }

        public void UnbindFromWriting()
        {
            this.FBO.Unbind(FramebufferTarget.DrawFramebuffer);

            // generate any requested mipmaps
            foreach (var ts in this.TextureSlots.Where(s => s.Enabled && s.TextureParam.MipMaps))
            {
                GL.Enable(EnableCap.Texture2D);
                ts.Texture.Bind();
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            }
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
                throw new InvalidOperationException(string.Format("GBuffer.SetSlotTextureParams: no texture at slot {0}", slot));
            }

            return this.TextureSlots[slot].Texture;
        }

        public Texture GetTextureAtSlotOrNull(int slot)
        {
            if (slot < 0 || slot >= MAXSLOTS)
            {
                return null;
            }
            if (this.TextureSlots[slot] == null || !this.TextureSlots[slot].Enabled)
            {
                return null;
            }

            return this.TextureSlots[slot].Texture;
        }

        public void ClearColourBuffer(int drawBuffer, Vector4 colour)
        {
            this.FBO.ClearColourBuffer(drawBuffer, colour);
        }

    }
}
