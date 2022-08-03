using EgsDbTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GlobalStructureListTests
{
    [TestClass]
    public class GSLTests
    {
        [TestMethod]
        public void ReadGlobalStructureList()
        {
            var gsla = new GlobalStructureListAccess();
            gsla.GlobalDbPath = @"C:\steamcmd\empyrion\Saves\Games\GigaGamingNeue Era V1+\global.db";
            var gsl = gsla.CurrentList;
        }
    }
}
