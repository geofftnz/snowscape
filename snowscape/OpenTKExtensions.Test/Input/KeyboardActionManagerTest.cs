using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTKExtensions.Input;
using OpenTK.Input;

namespace OpenTKExtensions.Test.Input
{
    [TestClass]
    public class KeyboardActionManagerTest
    {
        protected KeyboardActionManager Init()
        {
            return new KeyboardActionManager();
        }

        [TestMethod]
        public void has_zero_items_initially()
        {
            var sut = Init();
            Assert.AreEqual(0, sut.Count);
        }

        [TestMethod]
        public void can_add_item()
        {
            var sut = Init();
            sut.Add(Key.A, 0, () => { });
            Assert.AreEqual(1, sut.Count);
        }

        [TestMethod]
        public void can_add_items_with_same_key()
        {
            var sut = Init();
            sut.Add(Key.A, 0, () => { });
            sut.Add(Key.A, 0, () => { });
            Assert.AreEqual(2, sut.Count);
        }

        [TestMethod]
        public void can_process_keydown()
        {
            var sut = Init();
            bool sentinel = false;

            sut.Add(Key.A, 0, () => { sentinel = true; });
            sut.ProcessKeyDown(Key.A, 0);
            Assert.IsTrue(sentinel);
        }
        [TestMethod]
        public void can_process_keydown_multiple()
        {
            var sut = Init();
            bool sentinel = false;
            bool sentinel2 = false;

            sut.Add(Key.A, 0, () => { sentinel = true; });
            sut.Add(Key.A, 0, () => { sentinel2 = true; });

            sut.ProcessKeyDown(Key.A, 0);

            Assert.IsTrue(sentinel);
            Assert.IsTrue(sentinel2);
        }

    }
}
