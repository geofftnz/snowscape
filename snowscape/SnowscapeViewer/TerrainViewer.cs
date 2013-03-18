using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;
using NLog;
using Utils;
using OpenTKExtensions.Camera;
using OpenTK.Input;


namespace Snowscape.Viewer
{
    public class TerrainViewer : GameWindow
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private Matrix4 overlayProjection = Matrix4.Identity;
        private Matrix4 overlayModelview = Matrix4.Identity;

        private Matrix4 terrainProjection = Matrix4.Identity;
        private Matrix4 terrainModelview = Matrix4.Identity;

        private Font font = new Font();
        private TextManager textManager = new TextManager("DefaultText", null);

        private FrameCounter frameCounter = new FrameCounter();
        private TextBlock frameCounterText = new TextBlock("fps", "", new Vector3(0.01f, 0.05f, 0.0f), 0.0005f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        private PerfMonitor perfmon = new PerfMonitor();

        private ICamera camera;

        private double globalTime = 0.0;

        private GBuffer gbuffer = new GBuffer("gbuffer1");

        // bounding box
        // needs: 
        // - Vertex VBO
        // - Texcoord VBO (boxcoord)
        // - Vertex Shader
        // - Fragment Shader
        private VBO vertexVBO = new VBO("bbvertex");
        private VBO boxcoordVBO = new VBO("bbboxcoord");
        private VBO indexVBO = new VBO("bbindex", BufferTarget.ElementArrayBuffer);
        private ShaderProgram boundingBoxProgram = new ShaderProgram("bb");
        private Texture heighttex = new Texture("height", 256, 256, TextureTarget.Texture2D, PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float);


        // gbuffer combine
        private VBO gbufferCombineVertexVBO = new VBO("gbvertex");
        private VBO gbufferCombineTexcoordVBO = new VBO("gbtexcoord");
        private VBO gbufferCombineIndexVBO = new VBO("gbindex", BufferTarget.ElementArrayBuffer);
        private ShaderProgram gbufferCombineProgram = new ShaderProgram("gb");




        public class CloseEventArgs : EventArgs { }
        public delegate void CloseEventHandler(object source, CloseEventArgs e);
        public event CloseEventHandler OnClose;

        public TerrainViewer()
            : base(640, 480, new GraphicsMode(), "Snowscape", GameWindowFlags.Default, DisplayDevice.Default, 3, 0, GraphicsContextFlags.Default)
        {
            this.Load += new EventHandler<EventArgs>(TerrainViewer_Load);
            this.Closed += new EventHandler<EventArgs>(TerrainViewer_Closed);
            this.UpdateFrame += new EventHandler<FrameEventArgs>(TerrainViewer_UpdateFrame);
            this.RenderFrame += new EventHandler<FrameEventArgs>(TerrainViewer_RenderFrame);
            this.Resize += new EventHandler<EventArgs>(TerrainViewer_Resize);
        }

        void TerrainViewer_Resize(object sender, EventArgs e)
        {
            SetProjection();
        }

        private void SetProjection()
        {
            GL.Viewport(this.ClientRectangle);
            SetOverlayProjection();
            SetTerrainProjection();
        }

        private void SetOverlayProjection()
        {
            this.overlayProjection = Matrix4.CreateOrthographicOffCenter(0.0f, (float)this.ClientRectangle.Width / (float)this.ClientRectangle.Height, 1.0f, 0.0f, 0.001f, 10.0f);
            this.overlayModelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);
        }

        private void SetTerrainProjection()
        {
            this.terrainProjection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI * 0.4f, (float)this.ClientRectangle.Width / (float)this.ClientRectangle.Height, 0.1f, 1000.0f);

            double r = 300.0f;
            double a = Math.IEEERemainder(globalTime * 0.05, 1.0) * 2.0 * Math.PI;
            Vector3 eye = new Vector3((float)(128.0 + r * Math.Cos(a)), 200.0f, (float)(128.0 + r * Math.Sin(a)));

            this.terrainModelview = Matrix4.LookAt(eye, new Vector3(128.0f, 0.0f, 128.0f), Vector3.UnitY);
        }

        private void SetupGBufferCombiner()
        {
            var vertex = new Vector3[4];
            var texcoord = new Vector2[4];
            uint[] index = {0,1,3,1,2,3};

            int i = 0;

            vertex[i] = new Vector3(0.0f,0.0f,0.0f);
            texcoord[i] = new Vector2(0.0f, 0.0f);
            i++;
            vertex[i] = new Vector3(1.0f, 0.0f, 0.0f);
            texcoord[i] = new Vector2(1.0f, 0.0f);
            i++;
            vertex[i] = new Vector3(1.0f, 1.0f, 0.0f);
            texcoord[i] = new Vector2(1.0f, 1.0f);
            i++;
            vertex[i] = new Vector3(0.0f, 1.0f, 0.0f);
            texcoord[i] = new Vector2(0.0f, 1.0f);
            i++;

            this.gbufferCombineVertexVBO.SetData(vertex);
            this.gbufferCombineTexcoordVBO.SetData(texcoord);
            this.gbufferCombineIndexVBO.SetData(index);

            this.gbufferCombineProgram.Init(
                @"../../../Resources/Shaders/GBufferCombine.vert".Load(),
                @"../../../Resources/Shaders/GBufferCombine.frag".Load(),
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "in_texcoord0") 
                });
        }

        private void SetupBoundingBox(float minHeight, float maxHeight)
        {
            float minx, maxx, minz, maxz;
            Vector3[] vertex = new Vector3[8];
            Vector3[] boxcoord = new Vector3[8];

            minx = minz = 0.0f;
            maxx = 256.0f; // width of tile
            maxz = 256.0f; // height of tile

            for (int i = 0; i < 8; i++)
            {
                vertex[i].X = (i & 0x02) == 0 ? ((i & 0x01) == 0 ? minx : maxx) : ((i & 0x01) == 0 ? maxx : minx);
                vertex[i].Y = ((i & 0x04) == 0 ? minHeight : maxHeight);
                vertex[i].Z = (i & 0x02) == 0 ? minz : maxz;

                //this.BoundingBoxRenderVertex[i].Color = new Color(this.BoundingBoxRenderVertex[i].Position.X, this.BoundingBoxRenderVertex[i].Position.Y, this.BoundingBoxRenderVertex[i].Position.Z, 255);
                //this.BoundingBoxVertex[i].Position = this.BoundingBoxRenderVertex[i].Position;

                boxcoord[i].X = (i & 0x02) == 0 ? ((i & 0x01) == 0 ? minx : maxx) : ((i & 0x01) == 0 ? maxx : minx);
                boxcoord[i].Y = ((i & 0x04) == 0 ? minHeight : maxHeight);
                boxcoord[i].Z = (i & 0x02) == 0 ? minz : maxz;
            }

            // vertex VBO
            this.vertexVBO.SetData(vertex);
            // boxcoord VBO
            this.boxcoordVBO.SetData(boxcoord);

            // cubeindex VBO
            uint[] cubeindex = {
                                  7,3,2,
                                  7,2,6,
                                  6,2,1,
                                  6,1,5,
                                  5,1,0,
                                  5,0,4,
                                  4,3,7,
                                  4,0,3,
                                  3,1,2,
                                  3,0,1,
                                  5,7,6,
                                  5,4,7
                              };

            indexVBO.SetData(cubeindex);

            // setup shader
            this.boundingBoxProgram.Init(
                @"../../../Resources/Shaders/TerrainTile.vert".Load(),
                @"../../../Resources/Shaders/TerrainTile_Debug1.frag".Load(),
                new List<Variable> 
                { 
                    new Variable(0, "vertex"), 
                    new Variable(1, "in_boxcoord") 
                },
                new string[]
                {
                    "out_Pos",
                    "out_Normal",
                    "out_Shade",
                    "out_Param"
                });

            // init texture parameters
            this.heighttex
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

        }

        private void SetupHeightTexSampleData()
        {
            float[] height = new float[256 * 256];

            var r = new Random();
            float rx = (float)r.NextDouble();
            float ry = (float)r.NextDouble();

            ParallelHelper.For2D(256, 256, (x, y, i) =>
            {
                height[i] = Utils.SimplexNoise.wrapfbm((float)x, (float)y, 256f, 256f, rx, ry, 10, 0.2f / 256f, 200f, h => Math.Abs(h), h => h + h * h);
            });

            this.heighttex.Upload(height);
        }

        private void RenderBoundingBox(Matrix4 projection, Matrix4 modelview)
        {
            GL.Disable(EnableCap.Texture2D);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);
            
            this.heighttex.Bind(TextureUnit.Texture0);
            this.boundingBoxProgram.UseProgram();
            this.boundingBoxProgram.SetUniform("projection_matrix", projection);
            this.boundingBoxProgram.SetUniform("modelview_matrix", modelview);
            this.boundingBoxProgram.SetUniform("heightTex", 0);
            this.vertexVBO.Bind(this.boundingBoxProgram.VariableLocation("vertex"));
            this.boxcoordVBO.Bind(this.boundingBoxProgram.VariableLocation("in_boxcoord"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);

        }

        private void RenderGBufferCombiner(Matrix4 projection, Matrix4 modelview)
        {
            // bind the FBO textures
            this.gbuffer.GetTextureAtSlot(0).Bind(TextureUnit.Texture0);
            this.gbuffer.GetTextureAtSlot(1).Bind(TextureUnit.Texture1);
            this.gbuffer.GetTextureAtSlot(2).Bind(TextureUnit.Texture2);
            this.gbuffer.GetTextureAtSlot(3).Bind(TextureUnit.Texture3);

            this.gbufferCombineProgram
                .UseProgram()
                .SetUniform("projection_matrix", projection)
                .SetUniform("modelview_matrix", modelview)
                .SetUniform("posTex", 0)
                .SetUniform("normalTex", 1)
                .SetUniform("shadeTex", 2)
                .SetUniform("paramTex", 3);
            this.gbufferCombineVertexVBO.Bind(this.gbufferCombineProgram.VariableLocation("vertex"));
            this.gbufferCombineTexcoordVBO.Bind(this.gbufferCombineProgram.VariableLocation("in_texcoord0"));
            this.gbufferCombineIndexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.gbufferCombineIndexVBO.Length, DrawElementsType.UnsignedInt, 0);

        }

        void TerrainViewer_Load(object sender, EventArgs e)
        {
            this.VSync = VSyncMode.Off;

            // create VBOs/Shaders etc

            // GL state
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(new Color4(0, 24, 64, 255));


            // setup font
            font.Init(Resources.FontConsolas, Resources.FontConsolasMeta);
            textManager.Font = font;

            // setup camera
            // this.camera = new QuaternionCamera(Mouse, Keyboard, this, new Vector3(), new Quaternion(), true);

            this.SetupBoundingBox(0.0f, 128.0f);
            this.SetupHeightTexSampleData();

            this.SetupGBufferCombiner();
            this.gbuffer.SetSlot(0, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // pos
            this.gbuffer.SetSlot(1, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // normal
            this.gbuffer.SetSlot(2, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // shade
            this.gbuffer.SetSlot(3, new GBuffer.TextureSlotParam(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat));  // param
            this.gbuffer.Init(this.ClientRectangle.Width, this.ClientRectangle.Height);

            this.frameCounter.Start();
        }

        void TerrainViewer_UpdateFrame(object sender, FrameEventArgs e)
        {
            //this.camera.Update(e.Time);

            globalTime += e.Time;

            if (Keyboard[Key.Escape])
            {
                this.Close();
            }
        }

        void TerrainViewer_RenderFrame(object sender, FrameEventArgs e)
        {
            if (this.frameCounter.Frames % 32 == 0)
            {
                frameCounterText.Text = string.Format("FPS: {0:0}", frameCounter.FPSSmooth);
                textManager.AddOrUpdate(frameCounterText);

                float y = 0.1f;
                foreach (var timer in this.perfmon.AllAverageTimes())
                {
                    textManager.AddOrUpdate(new TextBlock("perf" + timer.Item1, string.Format("{0}: {1:0.000} ms", timer.Item1, timer.Item2), new Vector3(0.01f, y, 0.0f), 0.00025f, new Vector4(1.0f, 1.0f, 1.0f, 0.5f)));
                    y += 0.0125f;
                }
            }

            /*
            this.camera.GetProjectionMatrix(out this.terrainProjection);
            this.camera.GetModelviewMatrix(out this.terrainModelview);
             * */

            SetTerrainProjection();

            textManager.AddOrUpdate(new TextBlock("pmat", this.terrainProjection.ToString(), new Vector3(0.01f, 0.2f, 0.0f), 0.0003f, new Vector4(1.0f, 1.0f, 1.0f, 0.5f)));
            textManager.AddOrUpdate(new TextBlock("mvmat", this.terrainModelview.ToString(), new Vector3(0.01f, 0.25f, 0.0f), 0.0003f, new Vector4(1.0f, 1.0f, 1.0f, 0.5f)));


            this.gbuffer.BindForWriting();

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            perfmon.Start("RenderBox");
            this.RenderBoundingBox(this.terrainProjection, this.terrainModelview);
            perfmon.Stop("RenderBox");

            this.gbuffer.UnbindFromWriting();


            GL.Viewport(this.ClientRectangle);
            GL.ClearColor(0.0f, 0.0f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            perfmon.Start("RenderGBufferCombiner");
            RenderGBufferCombiner(overlayProjection, overlayModelview);
            perfmon.Stop("RenderGBufferCombiner");

            GL.Disable(EnableCap.DepthTest);
            perfmon.Start("RefreshText");
            if (textManager.NeedsRefresh) textManager.Refresh();
            perfmon.Stop("RefreshText");

            perfmon.Start("RenderText");
            textManager.Render(overlayProjection, overlayModelview);
            perfmon.Stop("RenderText");
            GL.Enable(EnableCap.DepthTest);

            GL.Flush();
            SwapBuffers();

            this.frameCounter.Frame();
        }


        void TerrainViewer_Closed(object sender, EventArgs e)
        {
            OnClose(this, new CloseEventArgs());
        }



    }
}
