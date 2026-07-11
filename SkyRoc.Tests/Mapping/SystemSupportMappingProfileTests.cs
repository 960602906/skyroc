using Application.DTOs.System;
using Application.Mappers;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.System;
using Xunit;

namespace SkyRoc.Tests.Mapping;

/// <summary>验证系统支撑实体到响应模型的机械映射配置。</summary>
public class SystemSupportMappingProfileTests
{
    [Fact]
    public void SystemSupportMappingProfile_MapsAllResponseModels()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<SystemSupportMappingProfile>());
        configuration.AssertConfigurationIsValid();
        var mapper = configuration.CreateMapper();
        var id = Guid.NewGuid();

        var period = mapper.Map<ServicePeriodDto>(new ServicePeriod
        {
            Id = id,
            Name = "午间配送",
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            SortOrder = 2
        });
        var operation = mapper.Map<OperationLogDto>(new OperationLog
        {
            Id = id,
            Module = "notices",
            OperationType = "Update",
            Desc = "PUT /api/notices/1",
            Method = "PUT",
            Url = "/api/notices/1",
            IpAddress = "127.0.0.1",
            IsSuccess = true
        });

        Assert.Equal((id, "午间配送", 2), (period.Id, period.Name, period.SortOrder));
        Assert.Equal((id, "notices", "Update"), (operation.Id, operation.Module, operation.OperationType));
        Assert.NotNull(mapper.Map<NoticeDto>(new Notice { Title = "提醒", Content = "正文" }));
        Assert.NotNull(mapper.Map<LoginLogDto>(new LoginLog { Username = "admin", IpAddress = "127.0.0.1" }));
    }
}
