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
            .ForMember(r => r.RoleName, opt => opt.MapFrom(r => r.Name))
            .ForMember(r => r.RoleCode, opt => opt.MapFrom(r => r.Code))
            .ForMember(r => r.RoleDesc, opt => opt.MapFrom(r => r.Desc));
        CreateMap<CreateRoleDto, Role>()
            .ForMember(r => r.Name, opt => opt.MapFrom(r => r.RoleName))
            .ForMember(r => r.Code, opt => opt.MapFrom(r => r.RoleCode))
            .ForMember(r => r.Desc, opt => opt.MapFrom(r => r.RoleDesc));
        CreateMap<UpdateRoleDto, Role>()
            .ForMember(r => r.Name, opt => opt.MapFrom(r => r.RoleName))
            .ForMember(r => r.Code, opt => opt.MapFrom(r => r.RoleCode))
            .ForMember(r => r.Desc, opt => opt.MapFrom(r => r.RoleDesc));
    }
}