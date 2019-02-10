using System;
using Eleon.Modding;

namespace EmpyrionModWebHost.Models
{
    public class PlayfieldGlobalStructureInfo
    {
        public string structureName;
        public string Playfield;
        public int Id;
        public string Name;
        public string Type;
        public int Faction;
        public int Blocks;
        public int Devices;
        public PVector3 Pos;
        public PVector3 Rot;
        public bool Core;
        public bool Powered;
        public bool Docked;
        public DateTime Touched_time;
        public int Touched_ticks;
        public string Touched_name;
        public int Touched_id;
        public DateTime Saved_time;
        public int Saved_ticks;
        public string Add_info;
    }
}