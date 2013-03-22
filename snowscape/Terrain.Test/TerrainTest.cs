using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Terrain.Test
{
    [TestClass]
    public class TerrainTest
    {
        [TestMethod]
        public void C_index_func_works_correctly_for_1024()
        {
            var t = new Terrain(1024, 1024);

            Assert.IsNotNull(t.C);
            Assert.AreEqual(1023, t.C(1023, 0));
            Assert.AreEqual(0, t.C(1024, 0));
            Assert.AreEqual(1024, t.C(0, 1));
            Assert.AreEqual(0, t.C(0, 1024));
            Assert.AreEqual(1024 * 1024 - 1, t.C(-1, -1));
            Assert.AreEqual(0, t.C(1024, 1024));
        }
        [TestMethod]
        public void C_index_func_works_correctly_for_256()
        {
            var t = new Terrain(256, 256);

            Assert.IsNotNull(t.C);
            Assert.AreEqual(255, t.C(255, 0));
            Assert.AreEqual(0, t.C(256, 0));
            Assert.AreEqual(256, t.C(0, 1));
            Assert.AreEqual(0, t.C(0, 256));
            Assert.AreEqual(256 * 256 - 1, t.C(-1, -1));
            Assert.AreEqual(0, t.C(256, 256));
        }
        [TestMethod]
        public void C_index_func_works_correctly_for_arbitrary()
        {
            var t = new Terrain(100, 100);

            Assert.IsNotNull(t.C);
            Assert.AreEqual(99, t.C(99, 0));
            Assert.AreEqual(0, t.C(100, 0));
            Assert.AreEqual(100, t.C(0, 1));
            Assert.AreEqual(0, t.C(0, 100));
            Assert.AreEqual(100 * 100 - 1, t.C(-1, -1));
            Assert.AreEqual(0, t.C(100, 100));
        }

        [TestMethod]
        public void CX_index_func_works_correctly_for_arbitrary()
        {
            var t = new Terrain(100, 100);

            Assert.IsNotNull(t.CX);
            Assert.AreEqual(0, t.CX(0));
            Assert.AreEqual(1, t.CX(1));
            Assert.AreEqual(1, t.CX(1 + 200));
            Assert.AreEqual(99, t.CX(499));
        }
        [TestMethod]
        public void CY_index_func_works_correctly_for_arbitrary()
        {
            var t = new Terrain(100, 100);

            Assert.IsNotNull(t.CY);
            Assert.AreEqual(0, t.CY(0));
            Assert.AreEqual(0, t.CY(1));
            Assert.AreEqual(0, t.CY(99));
            Assert.AreEqual(1, t.CY(100));
            Assert.AreEqual(1, t.CY(199));
        }
    }
}
