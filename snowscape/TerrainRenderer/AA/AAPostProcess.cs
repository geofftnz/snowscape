using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utils;
using OpenTKExtensions.Framework;
using OpenTKExtensions.Generators;

namespace Snowscape.TerrainRenderer.AA
{

    /// <summary>
    /// Post-processing with an accumulation buffer for blending over frames
    /// 
    /// HDR Exposure renders into source gbuffer, which becomes 1 of the inputs to the destination gbuffer
    /// Destination gbuffer switches between 2 frame textures
    /// 
    /// Final render outputs latest accumulation texture. Additional AA / post-proccessing done at this point
    /// </summary>
    public class AAPostProcess : GameComponentBase, IReloadable, IResizeable
    {
        // source GBuffer for HDR step to render into 
        private GBuffer sourcegbuffer = new GBuffer("aa-src", false);


        private GBuffer destinationgbuffer = new GBuffer("aa-dest", false);
        private ShaderProgram program = new ShaderProgram("aa-post");
        private GBufferCombiner gbufferCombiner;
        public int Width { get; set; }
        public int Height { get; set; }
        public float FrameBlend { get; set; }
        private Matrix4 projection = Matrix4.Identity;
        private Matrix4 modelview = Matrix4.Identity;

        // final render
        private VBO vertexVBO;
        private VBO indexVBO;
        private ShaderProgram destinationprogram = new ShaderProgram("aa-dest");

        private Texture[] frame = new Texture[2];
        private int currentFrame = 0;


        public AAPostProcess(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.FrameBlend = 1.0f;

            this.Loading += AAPostProcess_Loading;

            this.projection = Matrix4.CreateOrthographicOffCenter(0.0f, 1.0f, 0.0f, 1.0f, 0.001f, 10.0f);
            this.modelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);

        }

        private void AAPostProcess_Loading(object sender, EventArgs e)
        {

            vertexVBO = ScreenTri.Vertices().ToVBO();
            indexVBO = ScreenTri.Indices().ToVBO();

            this.sourcegbuffer.SetSlot(0, new GBuffer.TextureSlotParam(TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte, false, new List<ITextureParameter>
                {
                    new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear),
                    new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear),
                    new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge),
                    new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge)
                }));
            this.sourcegbuffer.Init(this.Width, this.Height);

            InitTextures();

            this.destinationgbuffer.SetSlot(0, frame[0]);
            this.destinationgbuffer.Init(this.Width, this.Height);

            this.gbufferCombiner = new GBufferCombiner(this.sourcegbuffer);
            this.Reload();

        }

        private void InitTextures()
        {
            for (int i = 0; i < 2; i++)
            {
                InitFrameTexture(i);
            }
        }

        private void InitFrameTexture(int i)
        {
            if (frame[i] != null)
            {
                frame[i].Unload();
                frame[i] = null;
            }

            frame[i] = new Texture("frame" + i, this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);
            frame[i].SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
            frame[i].SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear));
            frame[i].SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge));
            frame[i].SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge));

            frame[i].UploadEmpty();
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

        private ShaderProgram LoadDestShader()
        {
            var program = new ShaderProgram(this.GetType().Name);
            program.Init(
                @"AAPost.glsl|vsout",
                @"AAPost.glsl|fsout",
                new List<Variable> 
                { 
                    new Variable(0, "vertex")
                });
            return program;
        }

        private void SetDestShader(ShaderProgram newprogram)
        {
            if (this.destinationprogram != null)
            {
                this.destinationprogram.Unload();
            }
            this.destinationprogram = newprogram;
            //this.gbufferCombiner.Maybe(gb => gb.CombineProgram = this.program);
        }


        public void Reload()
        {
            this.ReloadShader(this.LoadShader, this.SetShader, log);
            this.ReloadShader(this.LoadDestShader, this.SetDestShader, log);
        }

        public void Resize(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            this.sourcegbuffer.Maybe(gb => gb.Init(width, height));

            InitTextures();
            destinationgbuffer.Maybe(gb =>
            {
                gb.SetSlot(0, frame[0]);
                gb.Init(width, height);
            });
        }


        public void BindForWriting()
        {
            this.sourcegbuffer.BindForWriting();
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.ClearDepth(1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.ColorMask(true, true, true, true);
        }

        public void UnbindFromWriting()
        {
            this.sourcegbuffer.UnbindFromWriting();
        }


        private void BindDestForWritingTo(int f)
        {
            this.destinationgbuffer.BindForWritingTo(new GBuffer.TextureSlot(0, frame[f]));
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.ClearDepth(1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.ColorMask(true, true, true, true);
        }

        private void UnbindDestFromWriting()
        {
            this.destinationgbuffer.UnbindFromWriting();
        }

        public void Render(bool moving)
        {
            if (moving)
            {
                FrameBlend = 1.0f;
            }
            else
            {
                FrameBlend -= 0.1f;
                if (FrameBlend < 0.05f)
                    FrameBlend = 0.05f;
            }

            // Render source gbuffer into destination accumulation buffer

            currentFrame = 1 - currentFrame;  // switch frames
            BindDestForWritingTo(currentFrame);

            sourcegbuffer.GetTextureAtSlot(0).Bind(TextureUnit.Texture0);
            frame[1 - currentFrame].Bind(TextureUnit.Texture1);

            this.gbufferCombiner.Render(projection, modelview, (sp) =>
            {
                sp.SetUniform("inputTex", 0);
                sp.SetUniform("lastFrameTex", 1);
                sp.SetUniform("frameBlend", this.FrameBlend);
            });

            UnbindDestFromWriting();

            // render current frame out
            frame[currentFrame].Bind(TextureUnit.Texture0);

            this.destinationprogram
                .UseProgram()
                .SetUniform("inputTex", 0);

            this.vertexVBO.Bind(this.destinationprogram.VariableLocation("vertex"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);


        }

    }
}
