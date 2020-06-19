using System.Linq;
using EgsDbTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GlobalStructurList.UnitTests
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var gslAccess = new GlobalStructureListAccess() { 
                GlobalDbPath = @"..\..\Data\global.db"
            };

            var gsl = gslAccess.CurrentList;

            var check1 = gsl.globalStructures["Sunchodacil"].Single(S => S.name == "UCH Heidelberg");

            Assert.AreEqual(check1.pos.x, 664.0);
            Assert.AreEqual(check1.pos.y, 64.0);
            Assert.AreEqual(check1.pos.z, 1144.0);

        }
    }
}
