using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenTKExtensions.Exceptions
{
    /// <summary>
    /// Thrown when a shader does not compile. Gives more context around the error location.
    /// </summary>
    public class ShaderCompileException : Exception
    {
        #region internal classes
        public class ErrorLine
        {
            public int FileIndex { get; set; }
            public int LineNumber { get; set; }
            public string Error { get; set; }
            public string LineSource { get; set; }

            private static Regex ErrorMatch = new Regex(@"(\d+)\((\d+)\) : (.*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

            public static ErrorLine Parse(string s, IList<string> sourceLines)
            {
                ErrorLine e = new ErrorLine();

                foreach (Match match in ErrorMatch.Matches(s))
                {
                    if (match.Groups.Count == 4)
                    {
                        e.FileIndex = match.Groups[1].Value.ParseIntOrDefault(0);
                        e.LineNumber = match.Groups[2].Value.ParseIntOrDefault(0);
                        e.Error = match.Groups[3].Value;

                        if (e.LineNumber > 0 && e.LineNumber <= sourceLines.Count)
                        {
                            e.LineSource = sourceLines[e.LineNumber-1];
                        }
                    }
                }

                return e;
            }

            public override string ToString()
            {
                return string.Format("{0}({1}) : {2} --> {3}", this.FileIndex, this.LineNumber, this.Error, this.LineSource);
            }
        }
        #endregion




        public string DetailText { get; private set; }
        public string Source { get; private set; }
        public List<ErrorLine> ErrorLines { get; private set; }
        public string DetailedError
        {
            get
            {
                return ErrorLines.Select(e => e.ToString()).StringJoin("\n");
            }
        }

        public ShaderCompileException(string name, string infoLog, string source)
            : base(string.Format("Shader {0} did not compile.", name))
        {
            this.DetailText = infoLog;
            this.Source = source;

            this.ErrorLines = ExtractErrorLines(this.DetailText, this.Source).ToList();
        }

        private IEnumerable<ErrorLine> ExtractErrorLines(string infoLog, string source)
        {
            var sourceLines = source.AllLines().ToList();

            foreach (var line in infoLog.AllLines())
            {
                yield return ErrorLine.Parse(line, sourceLines);
            }
        }

        
    }
}
