using AutoMapper;
using Eleon.Modding;
using EmpyrionModWebHost.Models;

namespace EmpyrionModWebHost
{
    public class AutoMapping : Profile
    {
        public AutoMapping()
        {

            CreateMap<PVector3, Vector3Data>();
            CreateMap<Vector3Data, PVector3>();
            CreateMap<GlobalStructureInfo, GlobalStructureInfoData>();
            CreateMap<GlobalStructureInfoData, GlobalStructureInfo>();
            CreateMap<GlobalStructureList, GlobalStructureListData>();
            CreateMap<GlobalStructureListData, GlobalStructureList>();
        }
    }
}
