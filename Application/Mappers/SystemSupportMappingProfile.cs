using Application.DTOs.System;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.System;

namespace Application.Mappers;

/// <summary>系统支撑实体与公开响应模型之间的机械映射配置。</summary>
public class SystemSupportMappingProfile : Profile
{
    /// <summary>配置服务时段、公告、操作日志和登录日志响应映射。</summary>
    public SystemSupportMappingProfile()
    {
        CreateMap<ServicePeriod, ServicePeriodDto>();
        CreateMap<Notice, NoticeDto>();
        CreateMap<OperationLog, OperationLogDto>();
        CreateMap<LoginLog, LoginLogDto>();
    }
}
