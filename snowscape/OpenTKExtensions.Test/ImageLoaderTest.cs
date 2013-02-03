using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenTKExtensions.Test
{
    [TestClass]
    public class ImageLoaderTest
    {
        [TestMethod]
        public void TestChannelExtraction()
        {
            byte[] input = new byte[] {0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15};

            byte[] output = input.ExtractChannelFromRGBA(0);

            Assert.AreEqual(4, output.Length);
            Assert.AreEqual(0, output[0]);
            Assert.AreEqual(8, output[2]);
            Assert.AreEqual(4+8+12, output.Select(x=>(int)x).Sum());

        }
    }
}
