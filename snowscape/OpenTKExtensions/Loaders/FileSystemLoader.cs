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
        const string THISFILE = @".";

        public string BaseDirectory { get; set; }

        public FileSystemLoader()
            : this(".")
        {
        }

        public FileSystemLoader(string BaseDirectory)
        {
            this.BaseDirectory = BaseDirectory;
        }

        public SourceContent LoadRaw(string sourceName, string baseSourceName)
        {
            // split source name into parts before and after SPLITCHAR (|)
            // first part is actual filename
            // second part refers to a labelled subset of file.
            string[] parts = sourceName.Split(SPLITCHAR);
            string fileName = sourceName, partName = null;

            // sourceName included the split character (eg: "hello.glsl|part1"), so treat the first part as the filename and the second part as the part name to extract
            if (parts.Length > 1)
            {
                // if the source file name is THISFILE ("."), then it's a reference to the parent file
                fileName = parts[0].Equals(THISFILE) ? baseSourceName : parts[0];
                partName = parts[1];
            }

            string content = File.ReadAllText(GetFilePath(fileName)).ExtractPart(partName);

            return new SourceContent
            {
                Name = new SourceName { Name = fileName, Part = partName },
                Content = content
            };
        }

        public SourceContent Load(string sourceName, string baseSourceName)
        {

            return this.LoadRaw(sourceName, baseSourceName).Preprocess(MAXLEVELS, this);
        }

        private string GetFilePath(string fileName)
        {
            return Path.Combine(this.BaseDirectory, fileName);
        }

    }
}
