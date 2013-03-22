using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils;

namespace Utils.Test
{
    [TestClass]
    public class UtilsTest
    {
        [TestMethod]
        public void float_wrap_works()
        {
            Assert.AreEqual(1f, 1.0f.Wrap(4.0f));
            Assert.AreEqual(0f, 4.0f.Wrap(4.0f));
            Assert.AreEqual(1f, 5.0f.Wrap(4.0f));
            Assert.AreEqual(3f, (-1.0f).Wrap(4.0f));
            Assert.AreEqual(3f, (-5.0f).Wrap(4.0f));
        }
        [TestMethod]
        public void int_wrap_works()
        {
            Assert.AreEqual(1, 1.Wrap(4));
            Assert.AreEqual(0, 4.Wrap(4));
            Assert.AreEqual(1, 5.Wrap(4));
            Assert.AreEqual(3, (-1).Wrap(4));
        }
    }
}
