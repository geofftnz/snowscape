using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions.Framework;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions.Generators;

namespace OpenTKExtensions.Components.PostProcess
{
    /// <summary>
    /// Ping-pong buffer with decayed blend - good
    /// for temporal AA.
    /// </summary>
    public class BlendBuffer : GameComponentBase, IReloadable, IResizeable
    {
        // settings
        public float AlphaDecay { get; set; }
        public float MinAlpha { get; set; }
        public float MaxAlpha { get; set; }
        public int DownScale { get; set; }

        // source gbuffer
        public PixelInternalFormat SourcePixelInternalFormat { get; set; }
        public PixelFormat SourcePixelFormat { get; set; }
        public PixelType SourcePixelType { get; set; }

        // destination ping-pong buffers
        public PixelInternalFormat DestPixelInternalFormat { get; set; }
        public PixelFormat DestPixelFormat { get; set; }
        public PixelType DestPixelType { get; set; }

        // internals
        private GBuffer sourcegbuffer = new GBuffer("aa-src", false);
        private GBuffer destinationgbuffer = new GBuffer("aa-dest", false);
        private ShaderProgram program = new ShaderProgram("aa-post");
        private GBufferCombiner gbufferCombiner;
        private int Width { get { return this.BaseWidth / Math.Max(1, this.DownScale); } }
        private int Height { get { return this.BaseHeight / Math.Max(1, this.DownScale); } }
        private int BaseWidth { get; set; }
        private int BaseHeight { get; set; }
        public float FrameBlend { get; set; }
        private Matrix4 projection = Matrix4.Identity;
        private Matrix4 modelview = Matrix4.Identity;

        private OpenTKExtensions.Loaders.MemoryLoader shaderLoader = new Loaders.MemoryLoader();

        // final render
        private VBO vertexVBO;
        private VBO indexVBO;
        private ShaderProgram destinationprogram = new ShaderProgram("aa-dest");

        private Texture[] frame = new Texture[2];
        private int currentFrame = 0;



        public BlendBuffer(int width, int height, int bitsPerComponent)
        {
            this.BaseWidth = width;
            this.BaseHeight = height;
            this.FrameBlend = 1.0f;

            this.MinAlpha = 0.05f;
            this.AlphaDecay = 0.5f;
            this.MaxAlpha = 1.0f;
            this.DownScale = 1;

            SetStandardBufferFormat(bitsPerComponent);

            this.DefineShaders();

            this.Loading += BlendBuffer_Loading;

            this.projection = Matrix4.CreateOrthographicOffCenter(0.0f, 1.0f, 0.0f, 1.0f, 0.001f, 10.0f);
            this.modelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);
        }
        public BlendBuffer(int width, int height)
            : this(width, height, 8)
        {

        }

        private void SetStandardBufferFormat(int bits)
        {
            switch (bits)
            {
                case 8: SetStandard8bppFormat(); break;
                case 16: SetStandard16bppFormat(); break;
                case 32: SetStandard32bppFormat(); break;
                default: throw new InvalidOperationException("Invalid buffer depth: " + bits.ToString());
            }

        }

        private void SetStandard8bppFormat()
        {
            this.SourcePixelInternalFormat = PixelInternalFormat.Rgba;
            this.SourcePixelFormat = PixelFormat.Rgba;
            this.SourcePixelType = PixelType.UnsignedByte;

            this.DestPixelInternalFormat = PixelInternalFormat.Rgba;
            this.DestPixelFormat = PixelFormat.Rgba;
            this.DestPixelType = PixelType.UnsignedByte;
        }
        private void SetStandard16bppFormat()
        {
            this.SourcePixelInternalFormat = PixelInternalFormat.Rgba16f;
            this.SourcePixelFormat = PixelFormat.Rgba;
            this.SourcePixelType = PixelType.HalfFloat;

            this.DestPixelInternalFormat = PixelInternalFormat.Rgba16f;
            this.DestPixelFormat = PixelFormat.Rgba;
            this.DestPixelType = PixelType.HalfFloat;
        }
        private void SetStandard32bppFormat()
        {
            this.SourcePixelInternalFormat = PixelInternalFormat.Rgba32f;
            this.SourcePixelFormat = PixelFormat.Rgba;
            this.SourcePixelType = PixelType.Float;

            this.DestPixelInternalFormat = PixelInternalFormat.Rgba32f;
            this.DestPixelFormat = PixelFormat.Rgba;
            this.DestPixelType = PixelType.Float;
        }

        private void BlendBuffer_Loading(object sender, EventArgs e)
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
            frame[i].SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest));
            frame[i].SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest));
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
                },
                new string[] { },
                shaderLoader);
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
                },
                new string[] { },
                shaderLoader);
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

            this.BaseWidth = width;
            this.BaseHeight = height;

            this.sourcegbuffer.Maybe(gb => gb.Init(Width, Height));

            InitTextures();
            destinationgbuffer.Maybe(gb =>
            {
                gb.SetSlot(0, frame[0]);
                gb.Init(Width, Height);
            });

            this.modelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);
            this.FrameBlend = MaxAlpha;
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
                FrameBlend = MaxAlpha;
            }
            else
            {
                FrameBlend *= AlphaDecay;
                if (FrameBlend < MinAlpha)
                    FrameBlend = MinAlpha;
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

            GL.Viewport(0, 0, this.BaseWidth, this.BaseHeight);
            // render current frame out
            frame[currentFrame].Bind(TextureUnit.Texture0);

            this.destinationprogram
                .UseProgram()
                .SetUniform("inputTex", 0)
                .SetUniform("resolution", new Vector2((float)this.BaseWidth, (float)this.BaseHeight))
                .SetUniform("invresolution", new Vector2(1.0f / (float)this.BaseWidth, 1.0f / (float)this.BaseHeight));

            this.vertexVBO.Bind(this.destinationprogram.VariableLocation("vertex"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);


        }

        #region Shaders
        private void DefineShaders()
        {
            shaderLoader.Add("AAPost.glsl",
            @"
                //|vs
                #version 140

                in vec3 vertex;
                noperspective out vec2 tex0;

                void main() {

	                gl_Position = vec4(vertex.xy * 2.0 - 1.0,0.0,1.0);
	                tex0 = vertex.xy;
                }

                //|fs
                #version 140
                precision highp float;

                uniform sampler2D inputTex;
                uniform sampler2D lastFrameTex;
                uniform float frameBlend;

                noperspective in vec2 tex0;

                out vec4 out_Col;

                const vec3 luminance = vec3(0.2126,0.7152,0.0722);


                void main(void)
                {
	                float fblend = frameBlend;
	                vec3 c = textureLod(inputTex,tex0,0).rgb;
	                vec4 lf = textureLod(lastFrameTex,tex0,0);
	                vec3 outc = mix(lf.rgb,c.rgb,fblend);
	                out_Col = vec4(outc, dot(outc, luminance));
                }

                //|vsout
                #version 140

                in vec3 vertex;
                noperspective out vec2 tex0;

                void main() {

	                gl_Position = vec4(vertex.xy * 2.0 - 1.0,0.0,1.0);
	                tex0 = vertex.xy;
                }

                //|fsout
                #version 140
                precision highp float;

                uniform sampler2D inputTex;
                uniform vec2 resolution;
                uniform vec2 invresolution;
                noperspective in vec2 tex0;

                out vec4 out_Col;

                void main(void)
                {
                    float lineBlend = 1.0 - mod((tex0.y * resolution.y),2.0) * 0.25;
                    vec3 c = vec3(0.0);

                    //c += textureLod(inputTex,tex0 + vec2(0.0,-invresolution.y),0).rgb * -0.25; 
                    //c += textureLod(inputTex,tex0 + vec2(0.0, invresolution.y),0).rgb * -0.25; 
                    //c += textureLod(inputTex,tex0 + vec2(-invresolution.x,0.0),0).rgb * -0.25; 
                    //c += textureLod(inputTex,tex0 + vec2( invresolution.x,0.0),0).rgb * -0.25; 
                    c += textureLod(inputTex,tex0,0).rgb; 

	                c *= lineBlend;
	                out_Col = vec4(c.rgb,1.0);
                }

            ");
        }
        #endregion

    }
}
