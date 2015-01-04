using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions.Loaders;

namespace OpenTKExtensions
{

    public interface IShaderLoader
    {
        /// <summary>
        /// Loads a text resource without performing preprocessing
        /// </summary>
        /// <param name="sourceName"></param>
        /// <returns></returns>
        SourceContent LoadRaw(string sourceName, string baseSourceName = @"");

        /// <summary>
        /// Loads a text resource and performs preprocessing
        /// </summary>
        /// <param name="sourceName"></param>
        /// <returns></returns>
        SourceContent Load(string sourceName, string baseSourceName = @"");
    }
}
