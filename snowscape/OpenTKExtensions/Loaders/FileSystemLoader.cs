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
        const char SPLITCHAR = '|';

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
            // split source name into parts before and after SPLITCHAR (|)
            // first part is actual filename
            // second part refers to a labelled subset of file.
            string[] parts = sourceName.Split(SPLITCHAR);
            string fileName = sourceName, partName = null;

            if (parts.Length > 1)
            {
                partName = parts[1];
            }

            return File.ReadAllText(GetFilePath(fileName)).ExtractPart(partName); 
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
