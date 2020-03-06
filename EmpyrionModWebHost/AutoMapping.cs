using AutoMapper;
using EmpyrionModWebHost.Models;

namespace EmpyrionModWebHost
{
    public class AutoMapping : Profile
    {
        public class RoleFormatter : IValueConverter<Role, string>
        {
            public string Convert(Role source, ResolutionContext context)
                => source.ToString();
        }

        public AutoMapping()
        {
            CreateMap<User, UserDto>()
                .ForMember(d => d.Role, opt => opt.ConvertUsing(new RoleFormatter()));
        }
    }
}
