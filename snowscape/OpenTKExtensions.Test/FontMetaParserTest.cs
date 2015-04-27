using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTKExtensions;
using OpenTKExtensions.Text;

namespace OpenTKExtensions.Test
{
    [TestClass]
    public class FontMetaParserTest
    {

        const string exampleLine = @"char id=1     x=507   y=463   width=4     height=5     xoffset=-1.500    yoffset=1.600     xadvance=30.813      page=0  chnl=0";
        const float floatError = 0.00001f;

        [TestMethod]
        public void GetDelimitedFields_can_tokenize_key_value_pairs()
        {
            string input = @"sjhf iufh d test=hello sfkjh bob=dave asd";

            var result = input.GetDelimitedFields(' ', '=').ToList();

            Assert.AreEqual(2, result.Count);

            Assert.IsTrue(
                result.Contains(new Tuple<string, string>("test", "hello")),
                "Could not find first pair"
                );

            Assert.IsTrue(
                result.Contains(new Tuple<string, string>("bob", "dave")),
                "Could not find second pair"
                );
        }

        [TestMethod]
        public void TryParseFontCharacter_will_ignore_invalid_lines()
        {
            FontCharacter c = new FontCharacter();
            Assert.IsFalse(FontMetaParser.TryParseCharacterInfoLine(@"sfsdf", out c));
        }

        [TestMethod]
        public void TryParseFontCharacter_correctly_parses_valid_line()
        {
            FontCharacter c = new FontCharacter();
            bool result = FontMetaParser.TryParseCharacterInfoLine(exampleLine, out c);

            //@"char id=1     x=507   y=463   width=4     height=5     xoffset=-1.500    yoffset=1.600     xadvance=30.813      page=0  chnl=0";
            Assert.AreEqual(1, c.ID);
            Assert.IsTrue(Math.Abs(507.0f - c.TexcoordX) < floatError);
            Assert.IsTrue(Math.Abs(463.0f - c.TexcoordY) < floatError);
            Assert.IsTrue(Math.Abs(4.0f - c.TexcoordW) < floatError);
            Assert.IsTrue(Math.Abs(5.0f - c.TexcoordH) < floatError);
            Assert.IsTrue(Math.Abs(-1.5f - c.XOffset) < floatError);
            Assert.IsTrue(Math.Abs(1.6f - c.YOffset) < floatError);
            Assert.IsTrue(Math.Abs(30.813f - c.XAdvance) < floatError);

        }
    }
}
