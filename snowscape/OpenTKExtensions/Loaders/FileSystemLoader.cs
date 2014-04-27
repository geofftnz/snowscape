using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OpenTKExtensions.Loaders
{
    public class FileSystemLoader : IShaderLoader
    {
        const int MAXLEVELS = 32; // maximum level of #include levels

        public string BaseDirectory { get; set; }

        public FileSystemLoader()
            : this(".")
        {
        }

        public FileSystemLoader(string BaseDirectory)
        {
            this.BaseDirectory = BaseDirectory;
        }

        public string LoadRaw(string sourceName)
        {
            return File.ReadAllText(GetFilePath(sourceName));
        }

        public string Load(string sourceName)
        {
            return this.LoadRaw(sourceName).Preprocess(MAXLEVELS, this);
        }

        private string GetFilePath(string fileName)
        {
            return Path.Combine(this.BaseDirectory, fileName);
        }

    }
}
