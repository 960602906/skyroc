using Application.DTOs.System;
using Application.QueryParameters.System;
using Shared.Constants;

namespace Application.interfaces.System;

/// <summary>定义运营设置、公告和系统审计日志查询的应用用例。</summary>
public interface ISystemSupportService
{
    /// <summary>按启用状态和排序读取运营服务时段。</summary>
    /// <param name="includeDisabled">是否同时返回已停用时段。</param>
    /// <returns>按排序值和名称稳定排序的时段集合。</returns>
    Task<IReadOnlyList<ServicePeriodDto>> GetServicePeriodsAsync(bool includeDisabled);
    /// <summary>读取单个运营服务时段。</summary>
    /// <param name="id">服务时段主键。</param>
    /// <returns>服务时段详情。</returns>
    Task<ServicePeriodDto> GetServicePeriodAsync(Guid id);
    /// <summary>新建运营服务时段。</summary>
    /// <param name="dto">时段边界、顺序和启用状态。</param>
    /// <returns>新建后的时段。</returns>
    Task<ServicePeriodDto> CreateServicePeriodAsync(UpsertServicePeriodDto dto);
    /// <summary>完整更新运营服务时段。</summary>
    /// <param name="id">待更新时段主键。</param>
    /// <param name="dto">时段边界、顺序和启用状态。</param>
    /// <returns>更新后的时段。</returns>
    Task<ServicePeriodDto> UpdateServicePeriodAsync(Guid id, UpsertServicePeriodDto dto);
    /// <summary>删除运营服务时段。</summary>
    /// <param name="id">待删除时段主键。</param>
    Task DeleteServicePeriodAsync(Guid id);
    /// <summary>读取小程序下单设置；未配置时返回安全默认值。</summary>
    /// <returns>当前下单开关和提前天数。</returns>
    Task<MiniProgramOrderSettingsDto> GetMiniProgramOrderSettingsAsync();
    /// <summary>保存小程序下单设置。</summary>
    /// <param name="dto">经过范围校验的下单设置。</param>
    /// <returns>保存后的设置。</returns>
    Task<MiniProgramOrderSettingsDto> SaveMiniProgramOrderSettingsAsync(MiniProgramOrderSettingsDto dto);
    /// <summary>读取分拣权重设置；未配置时返回零权重默认值。</summary>
    /// <returns>当前分拣权重。</returns>
    Task<SortingWeightSettingsDto> GetSortingWeightSettingsAsync();
    /// <summary>保存分拣权重设置。</summary>
    /// <param name="dto">非负且最多四位小数的权重设置。</param>
    /// <returns>保存后的分拣权重。</returns>
    Task<SortingWeightSettingsDto> SaveSortingWeightSettingsAsync(SortingWeightSettingsDto dto);
    /// <summary>分页查询通知公告。</summary>
    /// <param name="current">从 1 开始的页码。</param>
    /// <param name="size">每页记录数。</param>
    /// <param name="includeDraft">是否同时返回草稿。</param>
    /// <returns>公告分页数据。</returns>
    Task<PagedResult<NoticeDto>> GetNoticesAsync(int current, int size, bool includeDraft);
    /// <summary>新建草稿通知公告。</summary>
    /// <param name="dto">标题和正文。</param>
    /// <returns>新建公告。</returns>
    Task<NoticeDto> CreateNoticeAsync(UpsertNoticeDto dto);
    /// <summary>更新草稿或已发布公告的正文，更新后保持现有发布状态。</summary>
    /// <param name="id">公告主键。</param>
    /// <param name="dto">标题和正文。</param>
    /// <returns>更新后的公告。</returns>
    Task<NoticeDto> UpdateNoticeAsync(Guid id, UpsertNoticeDto dto);
    /// <summary>发布或撤回公告。</summary>
    /// <param name="id">公告主键。</param>
    /// <param name="dto">目标发布状态。</param>
    /// <returns>状态更新后的公告。</returns>
    Task<NoticeDto> UpdateNoticeStatusAsync(Guid id, UpdateNoticeStatusDto dto);
    /// <summary>删除公告及其内容。</summary>
    /// <param name="id">公告主键。</param>
    Task DeleteNoticeAsync(Guid id);
    /// <summary>分页查询关键操作审计日志。</summary>
    /// <param name="query">模块、结果和时间范围筛选。</param>
    /// <returns>按发生时间倒序的操作日志分页结果。</returns>
    Task<PagedResult<OperationLogDto>> GetOperationLogsAsync(OperationLogQueryParameters query);
    /// <summary>分页查询登录审计日志。</summary>
    /// <param name="query">登录名、结果和时间范围筛选。</param>
    /// <returns>按登录时间倒序的登录日志分页结果。</returns>
    Task<PagedResult<LoginLogDto>> GetLoginLogsAsync(LoginLogQueryParameters query);
}
