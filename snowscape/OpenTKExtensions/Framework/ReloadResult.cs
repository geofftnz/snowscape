using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Framework
{
    public class ReloadResult
    {
        public string TypeName { get; set; }
        public string ProgramName { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }

        public ReloadResult()
        {
        }
    }
}
