using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;
using Utils;
using TerrainGeneration;
using System.Threading;
using System.IO;
using NLog;
using System.Diagnostics;

namespace Snowscape.TerrainGenerationViewer
{
    public class TerrainGenerationViewer : GameWindow
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        const int TileWidth = 1024;
        const int TileHeight = 1024;

        public TerrainGen Terrain { get; set; }

        private Matrix4 projection = Matrix4.Identity;
        private Matrix4 modelview = Matrix4.Identity;
        private ShaderProgram quadShader = new ShaderProgram("QuadShader");
        private VBO quadVertexVBO = new VBO("quadvertex", BufferTarget.ArrayBuffer);
        private VBO quadTexcoordVBO = new VBO("quadtexcoord", BufferTarget.ArrayBuffer);
        private VBO quadIndexVBO = new VBO("quadindex", BufferTarget.ElementArrayBuffer);
        
        private Texture heightTex;
        private VBO heightTexBuffer = new VBO("heightTex", BufferTarget.PixelUnpackBuffer);

        private Texture shadeTex;
        private VBO shadeTexBuffer = new VBO("shadeTex", BufferTarget.PixelUnpackBuffer);

        float[] heightTexData = new float[TileWidth * TileHeight];
        byte[] shadeTexData = new byte[TileWidth * TileHeight * 4];
        uint updateCounter = 0;
        private Font font = new Font();
        private TextManager textManager = new TextManager("DefaultText", null);

        private FrameCounter frameCounter = new FrameCounter();
        private TextBlock frameCounterText = new TextBlock("fps","",new Vector3(0.01f,0.05f,0.0f),0.0005f,new Vector4(1.0f,1.0f,1.0f,1.0f));

        private Thread updateThread;
        private bool killThread = false;
        private TerrainGen.Cell[] threadCopyMap;
        private TerrainGen.Cell[] threadRenderMap;
        private uint updateThreadIterations;
        private uint prevThreadIterations;
        private double updateThreadUpdateTime = 0.0;

        private string terrainPath = @"../../../../terrains/";


        private Vector3[] quadPos = new Vector3[]{
            new Vector3(0f,0f,0f),
            new Vector3(0f,1f,0f),
            new Vector3(1f,0f,0f),
            new Vector3(1f,1f,0f)
        };

        private Vector2[] quadTexCoord = new Vector2[]{
            new Vector2(0f,0f),
            new Vector2(0f,1f),
            new Vector2(1f,0f),
            new Vector2(1f,1f)
        };

        private uint[] quadIndex = new uint[] { 0, 1, 2, 3 };

        #region shaders
        private string vertexShaderSource = @"
#version 140
 
uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
in vec3 vertex;
in vec2 in_texcoord0;
out vec2 texcoord0;
 
void main() {
    gl_Position = projection_matrix * modelview_matrix * vec4(vertex, 1.0);
    texcoord0 = in_texcoord0;
}
        ";

        private string fragmentShaderSource = @"
#version 140
precision highp float;

uniform sampler2D heightTex;
uniform sampler2D shadeTex;

in vec2 texcoord0;
out vec4 out_Colour;

const float TEXEL = 1.0 / 1024.0;

vec3 getNormal(vec2 pos)
{
    float h1 = texture2D(heightTex,vec2(pos.x,pos.y-TEXEL)).r;
    float h2 = texture2D(heightTex,vec2(pos.x,pos.y+TEXEL)).r;
    float h3 = texture2D(heightTex,vec2(pos.x-TEXEL,pos.y)).r;
    float h4 = texture2D(heightTex,vec2(pos.x+TEXEL,pos.y)).r;

    return normalize(vec3(h4-h3,h2-h1,2.0*TEXEL));
}


void main(void)
{

	vec4 colH1 = vec4(0.3,0.247,0.223,1.0);
	vec4 colH2 = vec4(0.3,0.247,0.223,1.0);

	vec4 colL1 = vec4(0.41,0.39,0.16,1.0);
	vec4 colL2 = vec4(0.41,0.39,0.16,1.0);

	vec4 colW = vec4(0.7,0.8,1.0,1.0);
	vec4 colE = vec4(1.0,0.4,0.0,1.0);

	vec4 s = texture2D(shadeTex,texcoord0.st);
	float h = texture2D(heightTex,texcoord0.st).r;

	float looseblend = clamp(s.g * s.g * 8.0,0.0,1.0);
	vec4 col = mix(mix(colH1,colH2,h),mix(colL1,colL2,h),looseblend);

	vec4 colW0 = vec4(0.325,0.498,0.757,1.0);  // blue water
	vec4 colW1 = vec4(0.659,0.533,0.373,1.0);  // dirty water
	vec4 colW2 = vec4(1.4,1.4,1.4,1.0); // white water

	colW = mix(colW0,colW1,clamp(s.r*4.0,0,1));  // make water dirty->clean
	colW = mix(colW,colW2,s.a*0.2);  // white water

	col = mix(col,colE,clamp(s.a,0.0,0.2));
	col = mix(col,colW,clamp(s.b*s.b*16.0,0,0.6)); // water

    vec3 n = getNormal(texcoord0.st);
    vec3 l = normalize(vec3(0.4,0.6,0.2));

	float diffuse = clamp(dot(n,l) * 0.5 + 0.5,0,1);
	col *= (0.4 + 0.6 * diffuse);

    out_Colour = vec4(col.rgb,1.0);
  
}
        ";
        #endregion

        public class CloseEventArgs : EventArgs { }
        public delegate void CloseEventHandler(object source, CloseEventArgs e);
        public event CloseEventHandler OnClose;

        public TerrainGenerationViewer()
            : base(640, 480, new GraphicsMode(), "Snowscape", GameWindowFlags.Default, DisplayDevice.Default, 3, 1, GraphicsContextFlags.Default)
        {
            this.Terrain = new TerrainGen(1024, 1024);

            this.UpdateFrame += new EventHandler<FrameEventArgs>(TerrainGenerationViewer_UpdateFrame);
            this.RenderFrame += new EventHandler<FrameEventArgs>(TerrainGenerationViewer_RenderFrame);
            this.Load += new EventHandler<EventArgs>(TerrainGenerationViewer_Load);
            this.Unload += new EventHandler<EventArgs>(TerrainGenerationViewer_Unload);
            this.Resize += new EventHandler<EventArgs>(TerrainGenerationViewer_Resize);
            this.Closed += new EventHandler<EventArgs>(TerrainGenerationViewer_Closed);
            this.Closing += new EventHandler<System.ComponentModel.CancelEventArgs>(TerrainGenerationViewer_Closing);
        }

        void TerrainGenerationViewer_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // kill thread
            log.Trace("Sending kill signal to update thread");
            lock (this)
            {
                this.killThread = true;
            }

            // wait for thread to complete
            log.Trace("Waiting for thread to complete...");
            this.updateThread.Join(2000);
            if (this.updateThread.IsAlive)
            {
                log.Warn("Thread.Join timeout - aborting");
                this.updateThread.Abort();
            }
        }

        void TerrainGenerationViewer_Closed(object sender, EventArgs e)
        {
            OnClose(this, new CloseEventArgs());
        }

        /*
        protected override void OnClosed(EventArgs e)
        {
            OnClose(this, new CloseEventArgs());
            base.OnClosed(e);
        }*/




        void TerrainGenerationViewer_Load(object sender, EventArgs e)
        {
            this.VSync = VSyncMode.On;

            // create VBOs/Shaders etc

            // GL state
            GL.Enable(EnableCap.DepthTest);

            this.heightTex = new Texture("height", TileWidth, TileHeight, TextureTarget.Texture2D, PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float);
            this.heightTex
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat))
                .Upload(heightTexData);

            this.shadeTex = new Texture("shade", TileWidth, TileHeight, TextureTarget.Texture2D, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);
            this.shadeTex
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat))
                .Upload(shadeTexData);

            // setup texture data buffers
            this.heightTexBuffer.SetData(heightTexData);
            this.shadeTexBuffer.SetData(shadeTexData);


            // setup VBOs
            this.quadVertexVBO.SetData(this.quadPos);
            this.quadTexcoordVBO.SetData(this.quadTexCoord);
            this.quadIndexVBO.SetData(this.quadIndex);

            // setup shader
            quadShader.Init(this.vertexShaderSource, this.fragmentShaderSource, new List<Variable> { new Variable(0, "vertex"), new Variable(1, "in_texcoord0") });

            // setup font
            font.Init(Resources.FontConsolas, Resources.FontConsolasMeta);
            //font.Init(Resources.FontSegoeScript, Resources.FontSegoeScriptMeta);
            //font.Init(Resources.FontOCR, Resources.FontOCRMeta);
            textManager.Font = font;

            SetProjection();

            try
            {
                this.Terrain.Load(this.GetTerrainFileName(0));
            }
            catch (FileNotFoundException)
            {
                this.Terrain.InitTerrain1();
            }

            this.threadCopyMap = new TerrainGen.Cell[this.Terrain.Width * this.Terrain.Height];
            this.threadRenderMap = new TerrainGen.Cell[this.Terrain.Width * this.Terrain.Height];

            this.frameCounter.Start();

            log.Trace("Starting update thread...");
            this.updateThread = new Thread(new ThreadStart(this.UpdateThreadProc));
            this.updateThread.Start();
        }

        private void UpdateThreadProc()
        {
            bool killMe = false;
            uint iteration = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            double startTime = 0.0;
            double updateTime = 0.0;
            

            while (true)
            {
                // check for kill request.
                lock (this)
                {
                    killMe = this.killThread;
                }
                if (killMe)
                {
                    break;
                }

                startTime = sw.Elapsed.TotalMilliseconds;
                this.Terrain.ModifyTerrain();
                updateTime = sw.Elapsed.TotalMilliseconds - startTime;

                iteration++;

                if (iteration % 10 == 0)
                {
                    lock (this)
                    {
                        ParallelHelper.For2D(this.Terrain.Width, this.Terrain.Height, (i) =>
                        {
                            this.threadCopyMap[i] = this.Terrain.Map[i];
                        });
                        this.updateThreadIterations = iteration;
                        this.updateThreadUpdateTime = this.updateThreadUpdateTime * 0.5 + 0.5 * updateTime;
                    }
                }


                Thread.Sleep(1);

            }

        }

        protected void CopyMapDataFromUpdateThread()
        {
            lock (this)
            {
                ParallelHelper.For2D(this.Terrain.Width, this.Terrain.Height, (i) =>
                {
                    this.threadRenderMap[i] = this.threadCopyMap[i];
                });
            }
        }


        void TerrainGenerationViewer_Unload(object sender, EventArgs e)
        {
        }

        void TerrainGenerationViewer_Resize(object sender, EventArgs e)
        {
            SetProjection();
        }

        private void SetProjection()
        {
            GL.Viewport(this.ClientRectangle);

            this.projection = Matrix4.CreateOrthographicOffCenter(0.0f, (float)this.ClientRectangle.Width / (float)this.ClientRectangle.Height, 1.0f, 0.0f, 0.001f, 10.0f);
            this.modelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);

        }

        protected void UpdateHeightTexture()
        {
            
            heightTexBuffer.Bind();
            IntPtr p = heightTexBuffer.Map(BufferAccess.WriteOnly);
            unsafe
            {
                float* dest = (float *)p.ToPointer();
                if ((IntPtr)dest != IntPtr.Zero)
                {
                    float scale = 1.0f / 4096.0f;

                    ParallelHelper.For2D(TileWidth, TileHeight, (x, y, i) =>
                    {
                        dest[i] = (this.threadRenderMap[i].Height) * scale;
                    });
                }
            }
            heightTexBuffer.Unmap();
            heightTexBuffer.Unbind();
            //GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            //this.heightTex.RefreshImage(this.heightTexData);
            this.heightTex.RefreshImage(heightTexBuffer);
        }

        protected void UpdateShadeTexture()
        {
            
            shadeTexBuffer.Bind();
            IntPtr p = shadeTexBuffer.Map(BufferAccess.WriteOnly);
            unsafe
            {
                byte* dest = (byte*)p.ToPointer();

                if ((IntPtr)dest != IntPtr.Zero)
                {
                    ParallelHelper.For2D(TileWidth, TileHeight, (x, y, i) =>
                    {
                        int j = i << 2;

                        dest[j] = (byte)((this.threadRenderMap[i].Loose * 4.0f).ClampInclusive(0.0f, 255.0f));
                        dest[j + 1] = (byte)((this.threadRenderMap[i].MovingWater * 2048.0f).ClampInclusive(0.0f, 255.0f));
                        dest[j + 2] = (byte)((this.threadRenderMap[i].Erosion * 32f).ClampInclusive(0.0f, 255.0f));  // erosion rate
                        dest[j + 3] = (byte)((this.threadRenderMap[i].Carrying * 32f).ClampInclusive(0.0f, 255.0f)); // carrying capacity
                    });
                }
            }
            shadeTexBuffer.Unmap();
            shadeTexBuffer.Unbind();
            this.shadeTex.RefreshImage(shadeTexBuffer);
        }

        void TerrainGenerationViewer_UpdateFrame(object sender, FrameEventArgs e)
        {
            //this.Terrain.ModifyTerrain();
            updateCounter++;
        }

        void TerrainGenerationViewer_RenderFrame(object sender, FrameEventArgs e)
        {

            this.Terrain.ModifyTerrain();

            frameCounterText.Text = string.Format("FPS: {0:0.00} {1} updates: {2:0.0}ms", frameCounter.FPSSmooth,this.updateThreadIterations, this.updateThreadUpdateTime);
            textManager.AddOrUpdate(frameCounterText);

            uint currentThreadIterations = updateThreadIterations;
            if (prevThreadIterations != currentThreadIterations)
            {
                CopyMapDataFromUpdateThread();
                UpdateShadeTexture();
                UpdateHeightTexture();
                prevThreadIterations = currentThreadIterations;
            }

            GL.ClearColor(new Color4(192, 208, 255, 255));
            GL.ClearDepth(10.0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            this.heightTex.Bind(TextureUnit.Texture0);
            this.shadeTex.Bind(TextureUnit.Texture1);
            quadShader.UseProgram();
            quadShader.SetUniform("projection_matrix", this.projection);
            quadShader.SetUniform("modelview_matrix", this.modelview);
            quadShader.SetUniform("heightTex", 0);
            quadShader.SetUniform("shadeTex", 1);
            quadVertexVBO.Bind(quadShader.VariableLocation("vertex"));
            quadTexcoordVBO.Bind(quadShader.VariableLocation("in_texcoord0"));
            quadIndexVBO.Bind();

            GL.DrawElements(BeginMode.TriangleStrip, quadIndexVBO.Length, DrawElementsType.UnsignedInt, 0);

            GL.Disable(EnableCap.DepthTest);
            textManager.Render(projection, modelview);
            GL.Enable(EnableCap.DepthTest);

            GL.Flush();

            SwapBuffers();

            this.frameCounter.Frame();
        }

        protected string GetTerrainFileName(int index)
        {
            return string.Format("{0}Terrain{1}.1024.pass1.ter", this.terrainPath, index);
        }

    }
}
