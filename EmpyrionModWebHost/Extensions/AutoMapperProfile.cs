using AutoMapper;
using EmpyrionModWebHost.Models;

namespace EmpyrionModWebHost.Extensions
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();
        }
    }
}