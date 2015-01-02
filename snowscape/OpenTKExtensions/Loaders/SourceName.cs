using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Loaders
{
    public class SourceName
    {
        const char SPLITCHAR = '|';
        const string THISFILE = @".";

        public string Name { get; set; }
        public string Part { get; set; }

        public SourceName()
        {
        }

        public SourceName(string namepart, string basename)
        {
            string[] parts = namepart.Split(SPLITCHAR);

            // sourceName included the split character (eg: "hello.glsl|part1"), so treat the first part as the filename and the second part as the part name to extract
            if (parts.Length > 1)
            {
                // if the source file name is THISFILE ("."), then it's a reference to the parent file
                Name = parts[0].Equals(THISFILE) ? basename : parts[0];
                Part = parts[1];
            }
            else
            {
                Name = namepart;
                Part = null;
            }

        }


    }
}
