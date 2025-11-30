using Application.DTOs.Auth;
using Application.DTOs.Menu;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappers;

/// <summary>
///     AutoMapper 配置文件 - 定义实体与 DTO 之间的映射关系
/// </summary>
public class MenuMappingProfile : Profile
{
    public MenuMappingProfile()
    {
        // ==================== Menu Mappings ====================
        CreateMap<Menu, MenuDto>()
            .ForMember(m => m.I18nKey, opt => opt.MapFrom(src => src.I18NKey));
        
        CreateMap<Menu, MenuTreeDto>()
            .ForMember(m => m.I18nKey, opt => opt.MapFrom(src => src.I18NKey))
            .ForMember(m => m.Children, opt => opt.Condition(src => src.Children.Count != 0));;
        
        CreateMap<List<Menu>, List<MenuTreeDto>>()
            .ConvertUsing((src, dest, ctx) => 
            {
                // 先映射所有
                var allDos = src.Select(m => ctx.Mapper.Map<MenuTreeDto>(m)).ToList();
                // 再过滤掉有 parentId 的顶层节点
                var rootDos = allDos.Where(m => m.ParentId is null)
                    .OrderBy(m => m.Order).ToList();
        
                return rootDos;
            });
        
        CreateMap<CreateMenuDto, Menu>();
        CreateMap<UpdateMenuDto, Menu>();

        
        // ==================== Route Mappings ====================
        CreateMap<Menu, RoutesHandleDto>()
            .ForMember(m => m.I18nKey, opt => opt.MapFrom(src => src.I18NKey));
        CreateMap<Menu, RoutesDto>()
            .ForMember(m => m.Handle, opt => opt.MapFrom(src => src))
            .ForMember(m => m.Children, opt => opt.Condition(src => src.Children.Count != 0));
        CreateMap<List<Menu>, List<RoutesDto>>()
            .ConvertUsing((src, dest, ctx) => 
            {
                // 先映射所有
                var allDos = src.Select(m => ctx.Mapper.Map<RoutesDto>(m)).ToList();
                // 再过滤掉有 parentId 的顶层节点
                var rootDos = allDos.Where(m => m.ParentId is null)
                    .OrderBy(m => m.Handle.Order).ToList();
        
                return rootDos;
            });
    }
}