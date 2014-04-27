using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTKExtensions;
using OpenTKExtensions.Loaders;

namespace OpenTKExtensions.Test
{
    [TestClass]
    public class FileLoaderTest
    {
        [TestMethod]
        public void TestMethod1()
        {

            var loader = new FileSystemLoader("d:/test/shaders");

            string source = loader.Load("rootshader.frag");

            Assert.AreNotEqual(string.Empty, source);

        }
    }
}
