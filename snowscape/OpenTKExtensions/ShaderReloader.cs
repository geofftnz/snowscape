using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using OpenTKExtensions.Framework;

namespace OpenTKExtensions
{
    public static class ShaderReloader
    {

        public static bool ReloadShader(this IGameComponent component, Func<ShaderProgram> GetNew, Action<ShaderProgram> SetNew, Logger log = null)
        {
            try
            {
                ShaderProgram p = GetNew();

                if (p == null)
                {
                    throw new InvalidOperationException("ReloadShader() returned null, but didn't throw an exception");
                }

                SetNew(p);
                return true;
            }
            catch (Exception ex)
            {
                if (log != null)
                {
                    log.Warn("Could not reload shader program {0}: {1}", component.GetType().Name, ex.GetType().Name + ": " + ex.Message);
                }
            }
            return false;
        }
    }
}
