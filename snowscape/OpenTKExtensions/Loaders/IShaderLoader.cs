using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions
{

    public interface IShaderLoader
    {
        /// <summary>
        /// Loads a text resource without performing preprocessing
        /// </summary>
        /// <param name="sourceName"></param>
        /// <returns></returns>
        string LoadRaw(string sourceName);

        /// <summary>
        /// Loads a text resource and performs preprocessing
        /// </summary>
        /// <param name="sourceName"></param>
        /// <returns></returns>
        string Load(string sourceName);
    }
}
