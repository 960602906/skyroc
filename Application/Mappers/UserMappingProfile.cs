using Application.DTOs.Auth;
using Application.DTOs.User;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappers;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // ==================== User Mappings ====================
        CreateMap<User, UserDto>()
            .ForMember(u => u.UserName, opt => opt.MapFrom(src =>src.Username))
            .ForMember(u => u.UserGender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(u => u.UserEmail, opt => opt.MapFrom(src => src.Email))
            .ForMember(u => u.UserPhone, opt => opt.MapFrom(src => src.Phone));
        CreateMap<User, UserInfoDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Roles, opt => opt.Ignore()) // 角色需要单独处理
            .ForMember(dest => dest.Buttons, opt => opt.Ignore()); // 按钮权限需要单独处理
        CreateMap<CreateUserDto, User>()
            .ForMember(u => u.Username, opt => opt.MapFrom(src => src.UserName));
        CreateMap<UpdateUserDto, User>()
            .ForMember(dest => dest.PasswordHash,
                opt => opt.Ignore()); // 密码不在更新DTO中// Add user-related mapping configurations here
        CreateMap<ChangePasswordDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.NewPassword)); // 假设有密码哈希逻辑
    }
}