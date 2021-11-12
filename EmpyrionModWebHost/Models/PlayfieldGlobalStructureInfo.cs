using AutoMapper;

namespace EmpyrionModWebHost.Models
{
#pragma warning disable IDE1006 // Naming Styles
    public class Vector3Data
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }

    public class PlayfieldGlobalStructureInfo
    {
        public string structureName{ get; set; }
        public string Playfield{ get; set; }
        public int Id{ get; set; }
        public string Name{ get; set; }
        public string Type{ get; set; }
        public int Faction{ get; set; }
        public int Blocks{ get; set; }
        public int Devices{ get; set; }
        public Vector3Data Pos { get; set; }
        public Vector3Data Rot { get; set; }
        public bool Core{ get; set; }
        public bool Powered{ get; set; }
        public bool Docked{ get; set; }
        public DateTime Touched_time{ get; set; }
        public int Touched_ticks{ get; set; }
        public string Touched_name{ get; set; }
        public int Touched_id{ get; set; }
        public DateTime Saved_time{ get; set; }
        public int Saved_ticks{ get; set; }
        public string Add_info{ get; set; }
    }
#pragma warning restore IDE1006 // Naming Styles

    public class PlayfieldGlobalStructureInfoAutoMapping : Profile
    {
        public PlayfieldGlobalStructureInfoAutoMapping()
        {
            CreateMap<GlobalStructureInfoData, PlayfieldGlobalStructureInfo>()
                .ForMember(D => D.Faction,      opt => opt.MapFrom(src => src.factionId))
                .ForMember(D => D.Blocks,       opt => opt.MapFrom(src => src.cntBlocks))
                .ForMember(D => D.Devices,      opt => opt.MapFrom(src => src.cntDevices))
                .ForMember(D => D.Core,         opt => opt.MapFrom(src => src.coreType > 0))
                .ForMember(D => D.Touched_time, opt => opt.MapFrom(src => DateTime.FromBinary(src.lastVisitedUTC)))
            ;
        }
    }

}