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
using OpenTK.Input;

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
        private Texture shadeTex;

        float[] heightTexData = new float[TileWidth * TileHeight];
        byte[] shadeTexData = new byte[TileWidth * TileHeight * 4];
        uint updateCounter = 0;
        private Font font = new Font();
        private TextManager textManager = new TextManager("DefaultText", null);

        private FrameCounter frameCounter = new FrameCounter();
        private TextBlock frameCounterText = new TextBlock("fps", "", new Vector3(0.01f, 0.05f, 0.0f), 0.0005f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

        private Thread updateThread;
        private bool killThread = false;
        private bool pauseThread = false;
        private bool threadPaused = false;

        private bool KillThread
        {
            get
            {
                bool temp;
                lock (this)
                {
                    temp = this.killThread;
                }
                return temp;
            }
            set
            {
                lock (this)
                {
                    this.killThread = value;
                }
            }
        }
        private bool PauseThread
        {
            get
            {
                bool temp;
                lock (this)
                {
                    temp = this.pauseThread;
                }
                return temp;
            }
            set
            {
                lock (this)
                {
                    this.pauseThread = value;
                }
            }
        }
        private bool ThreadPaused
        {
            get
            {
                bool temp;
                lock (this)
                {
                    temp = this.threadPaused;
                }
                return temp;
            }
            set
            {
                lock (this)
                {
                    this.threadPaused = value;
                }
            }
        }

        private TerrainGen.Cell[] threadCopyMap;
        private TerrainGen.Cell[] threadRenderMap;
        private uint updateThreadIterations;
        private uint prevThreadIterations;
        private double updateThreadUpdateTime = 0.0;
        private long waterIterations = 0;
        private int textureUpdateCount = 0;

        private PerfMonitor perfmon = new PerfMonitor();

        private string terrainPath = @"../../../../terrains/";

        private Vector2 view_offset = Vector2.Zero;
        private float view_scale = 1.0f;


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

            log.Trace("Saving terrain into slot 0...");
            this.Terrain.Save(this.GetTerrainFileName(0));
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
            this.VSync = VSyncMode.Off;

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


            // setup VBOs
            this.quadVertexVBO.SetData(this.quadPos);
            this.quadTexcoordVBO.SetData(this.quadTexCoord);
            this.quadIndexVBO.SetData(this.quadIndex);

            // setup shader
            quadShader.Init(@"../../../Resources/Shaders/TerrainGeneration.vert".Load(), @"../../../Resources/Shaders/TerrainGeneration_Vis.frag".Load(), new List<Variable> { new Variable(0, "vertex"), new Variable(1, "in_texcoord0") });

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
            //bool killMe = false;
            bool pauseMe = false;
            uint iteration = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            double startTime = 0.0;
            double updateTime = 0.0;


            while (true)
            {
                // check for kill request.
                if (this.KillThread)
                {
                    break;
                }
                //lock (this)
                //{
                //    killMe = this.killThread;
                //}
                //if (killMe)
                //{
                //    break;
                //}

                bool pauseRequest = this.PauseThread;

                if (pauseMe != pauseRequest)
                {
                    pauseMe = pauseRequest;
                    this.ThreadPaused = pauseMe;
                }

                if (!pauseMe)
                {
                    startTime = sw.Elapsed.TotalMilliseconds;
                    this.Terrain.ModifyTerrain();
                    updateTime = sw.Elapsed.TotalMilliseconds - startTime;

                    iteration++;

                    if (iteration % 2 == 0)
                    {
                        lock (this)
                        {
                            ParallelHelper.CopySingleThreadUnrolled(this.Terrain.Map, this.threadCopyMap, TileWidth * TileHeight);
                            this.updateThreadIterations = iteration;
                            this.updateThreadUpdateTime = this.updateThreadUpdateTime * 0.5 + 0.5 * updateTime;
                            this.waterIterations = this.Terrain.WaterIterations;
                        }
                    }
                }
                Thread.Sleep(1);
            }

        }

        protected void CopyMapDataFromUpdateThread()
        {
            lock (this)
            {
                perfmon.Start("Copy2");
                ParallelHelper.CopySingleThreadUnrolled(this.threadCopyMap, this.threadRenderMap, TileWidth * TileHeight);
                perfmon.Stop("Copy2");
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
            perfmon.Start("UpdateHeightTexture");
            float m = 1.0f / 4096.0f;
            ParallelHelper.For2D(TileWidth, TileHeight, (i) =>
            {
                this.heightTexData[i] = (this.threadRenderMap[i].Height) * m;
            });
            perfmon.Stop("UpdateHeightTexture");
            perfmon.Start("UploadHeightTexture");
            this.heightTex.RefreshImage(this.heightTexData);
            perfmon.Stop("UploadHeightTexture");
        }

        protected void UpdateShadeTexture()
        {
            perfmon.Start("UpdateShadeTexture");
            ParallelHelper.For2D(TileWidth, TileHeight, (i) =>
            {
                int j = (i) << 2;

                this.shadeTexData[j] = (byte)((this.threadRenderMap[i].Loose * 4.0f).Clamp(0.0f, 255.0f));
                this.shadeTexData[j + 1] = (byte)((this.threadRenderMap[i].MovingWater * 2048.0f).Clamp(0.0f, 255.0f));
                this.shadeTexData[j + 2] = (byte)((this.threadRenderMap[i].Carrying * 32f).Clamp(0.0f, 255.0f)); // carrying

                //this.shadeTexData[j + 3] = (byte)((this.threadRenderMap[i].Erosion * 16f).Clamp(0.0f, 255.0f));  // misc - particle speed
                //this.shadeTexData[j + 3] = (byte)((this.threadRenderMap[i].Erosion * 255f).Clamp(0.0f, 255.0f));  // misc - particle death

                this.shadeTexData[j + 3] = (byte)((this.threadRenderMap[i].Erosion * 0.25f).Clamp(0.0f, 255.0f)); // misc - age
            });
            perfmon.Stop("UpdateShadeTexture");
            perfmon.Start("UploadShadeTexture");
            this.shadeTex.RefreshImage(this.shadeTexData);
            perfmon.Stop("UploadShadeTexture");
        }

        void TerrainGenerationViewer_UpdateFrame(object sender, FrameEventArgs e)
        {
            float moveRate = 0.02f * this.view_scale;
            if (Keyboard[Key.Plus])
            {
                this.view_scale *= 0.95f;
            }

            if (Keyboard[Key.Minus])
            {
                this.view_scale *= 1.05f;
            }

            if (Keyboard[Key.Left])
            {
                this.view_offset.X -= moveRate;
            }
            if (Keyboard[Key.Right])
            {
                this.view_offset.X += moveRate;
            }
            if (Keyboard[Key.Up])
            {
                this.view_offset.Y -= moveRate;
            }
            if (Keyboard[Key.Down])
            {
                this.view_offset.Y += moveRate;
            }

            this.view_offset.X = this.view_offset.X.Wrap(1.0f);
            this.view_offset.Y = this.view_offset.Y.Wrap(1.0f);

            if (Keyboard[Key.R])
            {
                if (updateCounter > 10)
                {
                    ResetTerrain();
                    ResetCounters();
                }
            }


            updateCounter++;
        }

        private void ResetCounters()
        {
            this.updateCounter = 0;
        }

        private void ResetTerrain()
        {
            log.Info("Resetting Terrain - pausing thread");
            this.PauseThread = true;

            while (!this.ThreadPaused)
            {
                Thread.Sleep(1);
            }
            log.Info("Resetting Terrain - thread paused");

            this.Terrain.ResetTerrain();

            log.Info("Resetting Terrain - restarting thread");
            this.PauseThread = false;
        }

        void TerrainGenerationViewer_RenderFrame(object sender, FrameEventArgs e)
        {

            if (this.frameCounter.Frames % 32 == 0)
            {
                frameCounterText.Text = string.Format("FPS: {0:0} {1:###0} updates: {2:0.0}ms {3:#,###,###,##0} water iterations.", frameCounter.FPSSmooth, this.updateThreadIterations, this.updateThreadUpdateTime, this.waterIterations);
                textManager.AddOrUpdate(frameCounterText);
                
                float y = 0.1f;
                foreach (var timer in this.perfmon.AllAverageTimes())
                {
                    textManager.AddOrUpdate(new TextBlock("perf" + timer.Item1, string.Format("{0}: {1:0.000} ms", timer.Item1, timer.Item2), new Vector3(0.01f, y, 0.0f), 0.00025f, new Vector4(1.0f, 1.0f, 1.0f, 0.5f)));
                    y += 0.0125f;
                }
            }

            uint currentThreadIterations = updateThreadIterations;
            if (prevThreadIterations != currentThreadIterations)
            {
                CopyMapDataFromUpdateThread();
                UpdateShadeTexture();
                UpdateHeightTexture();
                textureUpdateCount++;
                prevThreadIterations = currentThreadIterations;
            }

            GL.ClearColor(new Color4(192, 208, 255, 255));
            GL.ClearDepth(10.0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            perfmon.Start("RenderTerrain");
            this.heightTex.Bind(TextureUnit.Texture0);
            this.shadeTex.Bind(TextureUnit.Texture1);
            quadShader.UseProgram();
            quadShader.SetUniform("projection_matrix", this.projection);
            quadShader.SetUniform("modelview_matrix", this.modelview);
            quadShader.SetUniform("tex0_offset", this.view_offset);
            quadShader.SetUniform("tex0_scale", this.view_scale);
            quadShader.SetUniform("heightTex", 0);
            quadShader.SetUniform("shadeTex", 1);
            //quadShader.SetUniform("resolution", new Vector2(this.ClientSize.Width,this.ClientSize.Height));
            quadVertexVBO.Bind(quadShader.VariableLocation("vertex"));
            quadTexcoordVBO.Bind(quadShader.VariableLocation("in_texcoord0"));
            quadIndexVBO.Bind();
            GL.DrawElements(BeginMode.TriangleStrip, quadIndexVBO.Length, DrawElementsType.UnsignedInt, 0);
            perfmon.Stop("RenderTerrain");

            GL.Disable(EnableCap.DepthTest);
            perfmon.Start("RefreshText");
            if (textManager.NeedsRefresh) textManager.Refresh();
            perfmon.Stop("RefreshText");

            perfmon.Start("RenderText");
            textManager.Render(projection, modelview);
            perfmon.Stop("RenderText");
            GL.Enable(EnableCap.DepthTest);

            GL.Flush();

            SwapBuffers();

            this.frameCounter.Frame();

            Thread.Sleep(16);
        }

        protected string GetTerrainFileName(int index)
        {
            return string.Format("{0}Terrain{1}.1024.pass1.ter", this.terrainPath, index);
        }

    }
}
