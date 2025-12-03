using Application.DTOs.MenuButton;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappers;

public class MenuButtonMappingProfile : Profile
{
    public MenuButtonMappingProfile()
    {
        CreateMap<MenuButton, MenuButtonDto>();
        CreateMap<CreateMenuButtonDto, MenuButton>();
        CreateMap<UpdateMenuButtonDto, MenuButton>();
    }
}