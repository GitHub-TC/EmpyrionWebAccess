using AutoMapper;
using Eleon.Modding;

namespace EmpyrionModWebHost.Models
{
    public class ItemInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ItemStackDTO
    {
        public int id { get; set; }
        public int count { get; set; }
        public byte slotIdx { get; set; }
        public int ammo { get; set; }
        public int decay { get; set; }

    }

    public class IdItemStackDTO
    {
        public int id { get; set; }
        public ItemStackDTO itemStack { get; set; }
    }

    public class AutoMapping : Profile
    {
        public AutoMapping()
        {
            CreateMap<ItemStackDTO, ItemStack>();
            CreateMap<IdItemStackDTO, IdItemStack>();
        }
    }
}