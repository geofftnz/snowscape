using OpenTKExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace OpenTKExtensions.Test
{
    
    
    /// <summary>
    ///This is a test class for SDFFontTest and is intended
    ///to contain all SDFFontTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SDFFontTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion



        /// <summary>
        ///A test for LoadMetaData
        ///</summary>
        [TestMethod()]
        public void LoadMetaDataTest()
        {
            Font target = new Font();
            
            string testInput = @"
info face=""Consolas""
chars count=193
char id=0     x=507   y=463   width=4     height=4     xoffset=-1.500    yoffset=1.500     xadvance=30.813      page=0  chnl=0
char id=13    x=507   y=459   width=4     height=4     xoffset=-1.500    yoffset=1.500     xadvance=30.813      page=0  chnl=0
char id=32    x=507   y=455   width=4     height=4     xoffset=-1.500    yoffset=1.500     xadvance=30.813      page=0  chnl=0
char id=33    x=496   y=348   width=12    height=43    xoffset=9.625     yoffset=40.125    xadvance=30.813      page=0  chnl=0
char id=34    x=44    y=489   width=21    height=17    xoffset=5.125     yoffset=40.125    xadvance=30.813      page=0  chnl=0
char id=35    x=204   y=209   width=32    height=39    xoffset=-0.375    yoffset=37.250    xadvance=30.813      page=0  chnl=0
char id=36    x=402   y=398   width=29    height=52    xoffset=0.938     yoffset=42.813    xadvance=30.813      page=0  chnl=0
char id=37    x=466   y=247   width=34    height=43    xoffset=-1.563    yoffset=40.625    xadvance=30.813      page=0  chnl=0
char id=38    x=432   y=247   width=34    height=43    xoffset=-0.625    yoffset=39.875    xadvance=30.813      page=0  chnl=0
char id=39    x=500   y=247   width=10    height=17    xoffset=10.563    yoffset=40.125    xadvance=30.813      page=0  chnl=0";

            using (var s = new MemoryStream(Encoding.UTF8.GetBytes(testInput)))
            {
                target.LoadMetaData(s);

                Assert.AreEqual(10, target.Characters.Count);
                Assert.IsTrue(target.Characters.ContainsKey((char)34));
                Assert.IsTrue(target.Characters.ContainsKey((char)38));
                Assert.IsTrue(target.Characters.ContainsKey((char)0));

                s.Close();
            }
        }

    }
}
