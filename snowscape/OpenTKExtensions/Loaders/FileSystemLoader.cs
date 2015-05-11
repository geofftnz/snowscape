using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OpenTKExtensions.Loaders
{
    public class FileSystemLoader : ShaderLoaderBase, IShaderLoader
    {

        public string BaseDirectory { get; set; }

        public FileSystemLoader()
            : this(".")
        {
        }

        public FileSystemLoader(string BaseDirectory)
        {
            this.BaseDirectory = BaseDirectory;
        }

        protected override string GetContent(string name)
        {
            return File.ReadAllText(GetFilePath(name));
        }

        private string GetFilePath(string fileName)
        {
            return Path.Combine(this.BaseDirectory, fileName);
        }

    }
}
