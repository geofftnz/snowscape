using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions.Framework;

namespace OpenTKExtensions
{
    public class GBufferSimpleStepComponent : GameComponentBase
    {
        private GBufferSimpleStep step;

        public GBufferSimpleStepComponent(string name, string fragmentSource, string outputTextureName, Texture outputTexture)
            : base()
        {
            step = new GBufferSimpleStep(name, fragmentSource, name + "tex", outputTextureName, outputTexture);
        }

        public override void Load()
        {
            this.LoadWrapper(() =>
            {
                base.Load();
                step.Init();
            });
        }

        public override void Unload()
        {
            this.UnloadWrapper(() =>
            {
                base.Unload();
            });
        }

        public void Render(Texture inputTexture, Action<ShaderProgram> SetUniforms = null)
        {
            step.Render(inputTexture, SetUniforms);
        }
    }
}
