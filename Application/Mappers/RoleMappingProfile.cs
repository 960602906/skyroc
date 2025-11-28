using Application.DTOs.Role;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappers;

public class RoleMappingProfile : Profile
{
    public RoleMappingProfile()
    {
        // ==================== Role Mappings ====================
        CreateMap<Role, RoleDto>()
            .ForMember(dest => dest.Menus, opt => opt.Ignore()); // 菜单权限需要单独处理
        CreateMap<CreateRoleDto, Role>();
        CreateMap<UpdateRoleDto, Role>();
    }
}