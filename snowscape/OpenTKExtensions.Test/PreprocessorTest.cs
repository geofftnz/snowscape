using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTKExtensions.Loaders;

namespace OpenTKExtensions.Test
{
    [TestClass]
    public class PreprocessorTest
    {
        [TestMethod]
        public void TestTokeniser()
        {

            string input = "hello #include \"test1\"  #include \"test2\" some more";

            var tokens = input.Tokens().ToArray();
            
            Assert.AreEqual(5, tokens.Length);
            Assert.AreEqual("test1", tokens[1].Content);
            Assert.AreEqual("test2", tokens[3].Content);
        }

        [TestMethod]
        public void TextPartExtract()
        {
            string input = 
@"Outside of parts

//|part1
this is part1
this is also part1
//|part2
this is part2";

            string part1 = input.ExtractPart("part1", "//|");
            string part2 = input.ExtractPart("part2", "//|");

            Assert.AreEqual(@"//|part1
this is part1
this is also part1
", part1);
            Assert.AreEqual(@"//|part2
this is part2
", part2);
        }
    }
}
