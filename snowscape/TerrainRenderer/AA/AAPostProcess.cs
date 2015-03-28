using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utils;
using OpenTKExtensions.Framework;

namespace Snowscape.TerrainRenderer.AA
{
    public class AAPostProcess : GameComponentBase, IReloadable, IResizeable
    {
        private GBuffer gbuffer = new GBuffer("aa", false);
        private ShaderProgram program = new ShaderProgram("aa");
        private GBufferCombiner gbufferCombiner;
        public int Width { get; set; }
        public int Height { get; set; }
        public float FrameBlend { get; set; }
        private Matrix4 projection = Matrix4.Identity;
        private Matrix4 modelview = Matrix4.Identity;



        public AAPostProcess(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.FrameBlend = 1.0f;

            this.Loading += AAPostProcess_Loading;

            this.projection = Matrix4.CreateOrthographicOffCenter(0.0f, 1.0f, 0.0f, 1.0f, 0.001f, 10.0f);
            this.modelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);

        }

        void AAPostProcess_Loading(object sender, EventArgs e)
        {
            this.gbuffer.SetSlot(0,
                new GBuffer.TextureSlotParam(TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte, false,
                new List<ITextureParameter>
                {
                    new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear),
                    new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear),
                    new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge),
                    new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge)
                }));  // colour
            this.gbuffer.Init(this.Width, this.Height);

            this.gbufferCombiner = new GBufferCombiner(this.gbuffer);
            this.Reload();


        }

        private ShaderProgram LoadShader()
        {
            var program = new ShaderProgram(this.GetType().Name);
            program.Init(
                @"AAPost.glsl|vs",
                @"AAPost.glsl|fs",
                new List<Variable> 
                { 
                    new Variable(0, "vertex")
                });
            return program;
        }

        private void SetShader(ShaderProgram newprogram)
        {
            if (this.program != null)
            {
                this.program.Unload();
            }
            this.program = newprogram;
            this.gbufferCombiner.Maybe(gb => gb.CombineProgram = this.program);
        }

        public void Reload()
        {
            this.ReloadShader(this.LoadShader, this.SetShader, log);
        }

        public void Resize(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            gbuffer.Maybe(gb => gb.Init(width, height));
        }

        public void BindForWriting()
        {
            this.gbuffer.BindForWriting();
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.ClearDepth(1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.ColorMask(true, true, true, true);
        }

        public void UnbindFromWriting()
        {
            this.gbuffer.UnbindFromWriting();
        }

        public void Render(bool moving)
        {
            if (moving)
            {
                FrameBlend = 1.0f;
            }
            else
            {
                FrameBlend *= 0.8f;
                if (FrameBlend < 0.1f)
                    FrameBlend = 0.1f;
            }


            this.gbufferCombiner.Render(projection, modelview, (sp) =>
            {
                sp.SetUniform("inputTex", 0);
                sp.SetUniform("lastFrameTex", 1);
                sp.SetUniform("frameBlend", this.FrameBlend);
            });
        }

    }
}
