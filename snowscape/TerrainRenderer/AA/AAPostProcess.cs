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
    public class AAPostProcess : GameComponentBase, IReloadable
    {
        private GBuffer gbuffer = new GBuffer("aa");
        private ShaderProgram program = new ShaderProgram("aa");
        private GBufferCombiner gbufferCombiner;


        public void Reload()
        {
            throw new NotImplementedException();
        }
    }
}
