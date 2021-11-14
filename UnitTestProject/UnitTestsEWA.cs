using EmpyrionModWebHost.Controllers;
using EmpyrionNetAPITools.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace UnitTestProject;

[TestClass]
public class UnitTestsEWA
{
    [TestMethod]
    public void TestReadItemInfos()
    {
        var ItemConfigFile = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..", "Data", "Config_Example.ecf");
        var LocalizationFile = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..", "Data", "Localization.csv");

        var items = new GameplayManager(null, null, NullLogger<GameplayManager>.Instance).ReadItemInfos(ItemConfigFile, LocalizationFile);

        Assert.AreEqual(1588, items.Length);
    }

    [TestMethod]
    public void TestMethodReadGenerateGlobalStructureInfo()
    {
        var s1 = BackupsController.GenerateGlobalStructureInfo(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..", "Data", "CV_Player_5295028.txt"));
        Assert.AreEqual(1378.068, Math.Round(s1.Pos.x, 3));
        Assert.AreEqual(-902.2444, Math.Round(s1.Pos.y, 4));
        Assert.AreEqual(-10097.43, Math.Round(s1.Pos.z, 2));

        Assert.AreEqual(358.5566, Math.Round(s1.Rot.x, 4));
        Assert.AreEqual(121.4577, Math.Round(s1.Rot.y, 4));
        Assert.AreEqual(359.9775, Math.Round(s1.Rot.z, 4));

        var s2 = BackupsController.GenerateGlobalStructureInfo(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..", "Data", "CV_Player_6130486.txt"));
        Assert.AreEqual(-4922.363, Math.Round(s2.Pos.x, 3));
        Assert.AreEqual(27.8112, Math.Round(s2.Pos.y, 4));
        Assert.AreEqual(2020.011, Math.Round(s2.Pos.z, 3));

        Assert.AreEqual(358.605, Math.Round(s2.Rot.x, 3));
        Assert.AreEqual(273.3854, Math.Round(s2.Rot.y, 4));
        Assert.AreEqual(3.907993, Math.Round(s2.Rot.z, 6));

        var s3 = BackupsController.GenerateGlobalStructureInfo(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..", "Data", "BA_Player_1077029.txt"));
        Assert.AreEqual(    280.5, Math.Round(s3.Pos.x, 1));   
        Assert.AreEqual(    112,   Math.Round(s3.Pos.y, 0));
        Assert.AreEqual(   -332.5, Math.Round(s3.Pos.z, 1));

        Assert.AreEqual(   0, Math.Round(s3.Rot.x, 0));   
        Assert.AreEqual( 270, Math.Round(s3.Rot.y, 0));
        Assert.AreEqual(   0, Math.Round(s3.Rot.z, 0));

    }

    [TestMethod]
    public void TestMethodReadFixSectors()
    {
        using var input = File.OpenText(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..", "Data", "FixSectors.yaml"));
        var SectorsData = YamlExtensions.YamlToObject<SectorsData>(input);
        var flattenSectors = SectorsManager.FlattenSectors(SectorsData);
        var origins = SectorsManager.ReadOrigins(SectorsData);
    }

    [TestMethod]
    public void TestMethodReadDynSectors()
    {
        var SectorsData = SectorsManager.ReadSectorFiles(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..", "Data"));
        var flattenSectors = SectorsManager.FlattenSectors(SectorsData);
        var origins = SectorsManager.ReadOrigins(SectorsData);
    }

    [TestMethod]
    public void TestGlobalStructureListDB()
    {
        var gsl = new EgsDbTools.GlobalStructureListAccess();
        gsl.GlobalDbPath = @"C:\steamcmd\empyrion.server\Saves\Games\DefaultRE\global.db";
        var result = gsl.CurrentList;

        Assert.IsTrue(result.globalStructures.Count > 0);
    }
}
