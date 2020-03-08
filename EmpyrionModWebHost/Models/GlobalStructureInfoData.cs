using System.Collections.Generic;

namespace EmpyrionModWebHost.Models
{
#pragma warning disable IDE1006 // Naming Styles
    public struct GlobalStructureInfoData
    {
        public int id { get; set; }
        public List<int> dockedShips { get; set; }
        public int classNr { get; set; }
        public int cntLights { get; set; }
        public int cntTriangles { get; set; }
        public int cntBlocks { get; set; }
        public int cntDevices { get; set; }
        public int fuel { get; set; }
        public bool powered { get; set; }
        public Vector3Data rot { get; set; }
        public Vector3Data pos { get; set; }
        public long lastVisitedUTC { get; set; }
        public string name { get; set; }
        public int factionId { get; set; }
        public byte factionGroup { get; set; }
        public byte type { get; set; }
        public sbyte coreType { get; set; }
        public int pilotId { get; set; }
    }

    public class GlobalStructureListData
    {
        public Dictionary<string, List<GlobalStructureInfoData>> globalStructures { get; set; }
    }
#pragma warning restore IDE1006 // Naming Styles
}
