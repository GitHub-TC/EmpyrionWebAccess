using EmpyrionModWebHost.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTestsEWA
    {
        [TestMethod]
        public void TestMethodReadGenerateGlobalStructureInfo()
        {
            var s1 = BackupsController.GenerateGlobalStructureInfo(Path.Combine(Directory.GetCurrentDirectory(),@"..\..\..", "Data", "CV_Player_5295028.txt"));
            var s2 = BackupsController.GenerateGlobalStructureInfo(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..", "Data", "CV_Player_6130486.txt"));
        }
    }
}
