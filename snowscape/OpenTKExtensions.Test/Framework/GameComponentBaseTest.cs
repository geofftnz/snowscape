using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTKExtensions.Framework;
using System.Collections.Generic;
using System.Linq;

namespace OpenTKExtensions.Test.Framework
{
    [TestClass]
    public class GameComponentBaseTest
    {
        private GameComponentBase sut = new GameComponentBase();
        private List<string> eventFired = new List<string>();

        public GameComponentBaseTest()
        {

        }


        [TestMethod]
        public void LoadingEventFired()
        {
            sut.Loading += sut_Loading;
            sut.Load();
            Assert.IsTrue(eventFired.Any(s => s.Equals("loading")));
            sut.Loading -= sut_Loading;
        }

        [TestMethod]
        public void LoadedEventFired()
        {
            sut.Loaded += sut_Loaded;
            sut.Load();
            Assert.IsTrue(eventFired.Any(s => s.Equals("loaded")));
            sut.Loaded -= sut_Loaded;
        }

        void sut_Loaded(object sender, EventArgs e)
        {
            eventFired.Add("loaded");
        }

        void sut_Loading(object sender, EventArgs e)
        {
            eventFired.Add("loading");
        }
    }
}
