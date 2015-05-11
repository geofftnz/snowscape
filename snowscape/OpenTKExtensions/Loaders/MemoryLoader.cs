using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Loaders
{
    /// <summary>
    /// Shader loader that serves up text from an internal buffer.
    /// Useful for self-contained utility renderers.
    /// </summary>
    public class MemoryLoader : ShaderLoaderBase, IShaderLoader
    {
        private Dictionary<string, string> shaderFiles = new Dictionary<string, string>();

        public MemoryLoader()
        {
        }

        public void Add(string name, string content)
        {
            lock (shaderFiles)
            {
                if (!shaderFiles.ContainsKey(name))
                    shaderFiles.Add(name, content);
            }
        }
            

        protected override string GetContent(string name)
        {
            string output;
            if (!shaderFiles.TryGetValue(name, out output))
                throw new InvalidOperationException("Could not find content for " + name);
            return output;
        }
    }
}
