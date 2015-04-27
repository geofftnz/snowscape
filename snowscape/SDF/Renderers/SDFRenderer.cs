using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTKExtensions;
using OpenTKExtensions.Camera;
using OpenTKExtensions.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDF.Renderers
{
    public class SDFRenderer : GameComponentBase, IRenderable, IResizeable, IReloadable
    {
        public bool Visible { get; set; }
        public int DrawOrder { get; set; }

        private VBO vertexVBO;// = new VBO("v");
        private VBO indexVBO;// = new VBO("i", BufferTarget.ElementArrayBuffer);
        private ShaderProgram program = null;
        public ICamera Camera { get; set; }


        private GameComponentCollection Components = new GameComponentCollection();



        public SDFRenderer()
        {
            Visible = true;
            DrawOrder = 0;

            this.Loading += SDFRenderer_Loading;
            this.Unloading += SDFRenderer_Unloading;
        }

        void SDFRenderer_Loading(object sender, EventArgs e)
        {
            vertexVBO = new Vector3[] { new Vector3(-1f, -1f, 0f), new Vector3(3f, -1f, 0f), new Vector3(-1f, 3f, 0f) }.ToVBO();
            indexVBO = new uint[] { 0, 1, 2 }.ToVBO();

            Reload();

            this.Components.Load();
        }


        void SDFRenderer_Unloading(object sender, EventArgs e)
        {

            this.Components.Unload();
        }



        public void Render(IFrameRenderData frameData)
        {
            var cam = Camera;
            if (cam == null) return;


            Matrix4 invProjectionView = Matrix4.Invert(Matrix4.Mult(cam.View, cam.Projection));

            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);

            this.program
                .UseProgram()
                .SetUniform("inverse_projectionview_matrix", invProjectionView)
                .SetUniform("eyePos", cam.Eye);
            this.vertexVBO.Bind(this.program.VariableLocation("vertex"));
            this.indexVBO.Bind();
            GL.DrawElements(BeginMode.Triangles, this.indexVBO.Length, DrawElementsType.UnsignedInt, 0);
        }

        public void Resize(int width, int height)
        {

        }


        private ShaderProgram LoadShader()
        {
            var program = new ShaderProgram(this.GetType().Name);
            program.Init(
                @"sdf.glsl|vs",
                @"sdf.glsl|fs",
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
        }

        public void Reload()
        {
            this.ReloadShader(this.LoadShader, this.SetShader, log);
        }
    }
}
