using Application.DTOs.Department;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappers;

public class DepartmentMappingProfile: Profile
{
    public  DepartmentMappingProfile()
    {
        CreateMap<Department, DepartmentDto>();
        CreateMap<Department, DepartmentTreeDto>()
            .ForMember(m => m.Children, opt => opt.MapFrom(src => src));

        CreateMap<CreateDepartmentDto, Department>();
        CreateMap<UpdateDepartmentDto, Department>();
    }
}