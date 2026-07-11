using Application.DTOs.Department;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappers;

public class DepartmentMappingProfile : Profile
{
    public DepartmentMappingProfile()
    {
        CreateMap<Department, DepartmentDto>();
        CreateMap<Department, DepartmentTreeDto>()
            .ForMember(m => m.Children, opt => opt.Ignore());

        CreateMap<List<Department>, List<DepartmentTreeDto>>()
            .ConvertUsing((src, dest, ctx) =>
            {
                var allDtos = src.Select(department => ctx.Mapper.Map<DepartmentTreeDto>(department)).ToList();
                var dtoMap = allDtos.ToDictionary(x => x.Id);

                foreach (var dto in allDtos)
                    if (dto.ParentId.HasValue && dtoMap.TryGetValue(dto.ParentId.Value, out var parent))
                    {
                        parent.Children ??= [];
                        parent.Children.Add(dto);
                    }

                return allDtos
                    .Where(x => x.ParentId is null)
                    .OrderBy(x => x.Sort)
                    .ToList();
            });

        CreateMap<CreateDepartmentDto, Department>();
        CreateMap<UpdateDepartmentDto, Department>();
    }
}
