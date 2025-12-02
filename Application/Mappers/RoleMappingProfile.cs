using Application.DTOs.Role;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappers;

public class RoleMappingProfile : Profile
{
    public RoleMappingProfile()
    {
        // ==================== Role Mappings ====================
        CreateMap<Role, RoleDto>();
        CreateMap<CreateRoleDto, Role>();
        CreateMap<UpdateRoleDto, Role>();
    }
}