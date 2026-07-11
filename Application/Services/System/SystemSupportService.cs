using System.Linq.Expressions;
using System.Text.Json;
using Application.DTOs.System;
using Application.Exceptions;
using Application.interfaces;
using Application.interfaces.System;
using Application.QueryParameters.System;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.System;
using Domain.Interfaces;
using Domain.Interfaces.System;
using Shared.Constants;

namespace Application.Services.System;

/// <summary>运营设置、通知公告和只读审计查询的应用服务。</summary>
public class SystemSupportService(
    IServicePeriodRepository servicePeriodRepository,
    ISystemSettingRepository systemSettingRepository,
    INoticeRepository noticeRepository,
    IOperationLogRepository operationLogRepository,
    ILoginLogRepository loginLogRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IMapper mapper) : ISystemSupportService
{
    private const int MaxNoticeContentLength = 20_000;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServicePeriodDto>> GetServicePeriodsAsync(bool includeDisabled)
    {
        var periods = includeDisabled
            ? await servicePeriodRepository.GetAllAsync()
            : await servicePeriodRepository.FindAsync(x => x.Status == Status.Enable);
        return mapper.Map<List<ServicePeriodDto>>(
            periods.OrderBy(x => x.SortOrder).ThenBy(x => x.Name, StringComparer.Ordinal));
    }

    /// <inheritdoc />
    public async Task<ServicePeriodDto> GetServicePeriodAsync(Guid id)
    {
        var entity = await GetServicePeriodEntityAsync(id);
        return mapper.Map<ServicePeriodDto>(entity);
    }

    /// <inheritdoc />
    public async Task<ServicePeriodDto> CreateServicePeriodAsync(UpsertServicePeriodDto dto)
    {
        var snapshot = ValidateServicePeriod(dto);
        if (await servicePeriodRepository.ExistsAsync(x => x.Name == snapshot.Name))
        {
            throw new BusinessException("服务时段名称已存在");
        }

        var period = new ServicePeriod
        {
            Id = Guid.NewGuid(),
            Name = snapshot.Name,
            StartTime = snapshot.StartTime,
            EndTime = snapshot.EndTime,
            SortOrder = snapshot.SortOrder,
            Status = snapshot.IsEnabled ? Status.Enable : Status.Disable,
            CreateBy = currentUserService.GetUserId(),
            CreateName = currentUserService.GetUserName()
        };
        await servicePeriodRepository.AddAsync(period);
        await SaveWithUniqueConstraintAsync("服务时段名称已存在");
        return mapper.Map<ServicePeriodDto>(period);
    }

    /// <inheritdoc />
    public async Task<ServicePeriodDto> UpdateServicePeriodAsync(Guid id, UpsertServicePeriodDto dto)
    {
        var snapshot = ValidateServicePeriod(dto);
        var period = await GetServicePeriodEntityAsync(id);
        if (await servicePeriodRepository.ExistsAsync(x => x.Id != id && x.Name == snapshot.Name))
        {
            throw new BusinessException("服务时段名称已存在");
        }

        period.Name = snapshot.Name;
        period.StartTime = snapshot.StartTime;
        period.EndTime = snapshot.EndTime;
        period.SortOrder = snapshot.SortOrder;
        period.Status = snapshot.IsEnabled ? Status.Enable : Status.Disable;
        ApplyUpdateAudit(period);
        await servicePeriodRepository.UpdateAsync(period);
        await SaveWithUniqueConstraintAsync("服务时段名称已存在");
        return mapper.Map<ServicePeriodDto>(period);
    }

    /// <inheritdoc />
    public async Task DeleteServicePeriodAsync(Guid id)
    {
        await servicePeriodRepository.DeleteAsync(await GetServicePeriodEntityAsync(id));
        await unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<MiniProgramOrderSettingsDto> GetMiniProgramOrderSettingsAsync()
    {
        return await GetSettingAsync(SystemSettingKey.MiniProgramOrder, new MiniProgramOrderSettingsDto());
    }

    /// <inheritdoc />
    public async Task<MiniProgramOrderSettingsDto> SaveMiniProgramOrderSettingsAsync(MiniProgramOrderSettingsDto dto)
    {
        if (dto.MaxAdvanceOrderDays is < 0 or > 30)
        {
            throw new BusinessException("提前下单天数必须在 0 到 30 之间");
        }
        return await SaveSettingAsync(SystemSettingKey.MiniProgramOrder, dto);
    }

    /// <inheritdoc />
    public async Task<SortingWeightSettingsDto> GetSortingWeightSettingsAsync()
    {
        return await GetSettingAsync(SystemSettingKey.SortingWeight, new SortingWeightSettingsDto());
    }

    /// <inheritdoc />
    public async Task<SortingWeightSettingsDto> SaveSortingWeightSettingsAsync(SortingWeightSettingsDto dto)
    {
        var weights = new[] { dto.OrderTimeWeight, dto.RouteWeight, dto.CustomerWeight };
        if (weights.Any(x => x < 0 || decimal.Round(x, NumericPrecision.MoneyScale, NumericPrecision.RoundingMode) != x))
        {
            throw new BusinessException("分拣权重必须为非负且最多保留 4 位小数");
        }
        return await SaveSettingAsync(SystemSettingKey.SortingWeight, dto);
    }

    /// <inheritdoc />
    public async Task<PagedResult<NoticeDto>> GetNoticesAsync(int current, int size, bool includeDraft)
    {
        EnsurePage(current, size);
        Expression<Func<Notice, bool>>? predicate = includeDraft ? null : x => x.NoticeStatus == NoticeStatus.Published;
        var (data, total) = await noticeRepository.GetPagedAsync(predicate, current, size, x => x.CreateTime!, true);
        return CreatePage(mapper.Map<List<NoticeDto>>(data), total, current, size);
    }

    /// <inheritdoc />
    public async Task<NoticeDto> CreateNoticeAsync(UpsertNoticeDto dto)
    {
        var snapshot = ValidateNotice(dto);
        var entity = new Notice
        {
            Id = Guid.NewGuid(),
            Title = snapshot.Title,
            Content = snapshot.Content,
            NoticeStatus = NoticeStatus.Draft,
            Status = Status.Enable,
            CreateBy = currentUserService.GetUserId(),
            CreateName = currentUserService.GetUserName()
        };
        await noticeRepository.AddAsync(entity);
        await unitOfWork.SaveChangesAsync();
        return mapper.Map<NoticeDto>(entity);
    }

    /// <inheritdoc />
    public async Task<NoticeDto> UpdateNoticeAsync(Guid id, UpsertNoticeDto dto)
    {
        var snapshot = ValidateNotice(dto);
        var entity = await GetNoticeEntityAsync(id);
        entity.Title = snapshot.Title;
        entity.Content = snapshot.Content;
        ApplyUpdateAudit(entity);
        await noticeRepository.UpdateAsync(entity);
        await unitOfWork.SaveChangesAsync();
        return mapper.Map<NoticeDto>(entity);
    }

    /// <inheritdoc />
    public async Task<NoticeDto> UpdateNoticeStatusAsync(Guid id, UpdateNoticeStatusDto dto)
    {
        if (!Enum.IsDefined(dto.NoticeStatus)) throw new BusinessException("公告状态不合法");
        var entity = await GetNoticeEntityAsync(id);
        entity.NoticeStatus = dto.NoticeStatus;
        entity.PublishedTime = dto.NoticeStatus == NoticeStatus.Published ? DateTime.UtcNow : null;
        ApplyUpdateAudit(entity);
        await noticeRepository.UpdateAsync(entity);
        await unitOfWork.SaveChangesAsync();
        return mapper.Map<NoticeDto>(entity);
    }

    /// <inheritdoc />
    public async Task DeleteNoticeAsync(Guid id)
    {
        await noticeRepository.DeleteAsync(await GetNoticeEntityAsync(id));
        await unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<PagedResult<OperationLogDto>> GetOperationLogsAsync(OperationLogQueryParameters query)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateTimeRange(query.StartTime, query.EndTime);
        var keyword = Normalize(query.Keyword)?.ToLowerInvariant();
        var module = Normalize(query.Module);
        var operationType = Normalize(query.OperationType);
        var (data, total) = await operationLogRepository.GetPagedAsync(x =>
            (module == null || x.Module == module) && (operationType == null || x.OperationType == operationType) &&
            (keyword == null || x.Desc.ToLower().Contains(keyword) || x.Url.ToLower().Contains(keyword) ||
             (x.RequestParams != null && x.RequestParams.ToLower().Contains(keyword)) ||
             (x.ResponseResult != null && x.ResponseResult.ToLower().Contains(keyword)) ||
             (x.ErrorMessage != null && x.ErrorMessage.ToLower().Contains(keyword)) ||
             (x.CreateName != null && x.CreateName.ToLower().Contains(keyword))) &&
            (!query.IsSuccess.HasValue || x.IsSuccess == query.IsSuccess) &&
            (!query.StartTime.HasValue || x.CreateTime >= query.StartTime) && (!query.EndTime.HasValue || x.CreateTime <= query.EndTime),
            query.Current, query.Size, x => x.CreateTime ?? DateTime.MinValue, true);
        return CreatePage(mapper.Map<List<OperationLogDto>>(data), total, query.Current, query.Size);
    }

    /// <inheritdoc />
    public async Task<PagedResult<LoginLogDto>> GetLoginLogsAsync(LoginLogQueryParameters query)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateTimeRange(query.StartTime, query.EndTime);
        var keyword = Normalize(query.Keyword)?.ToLowerInvariant();
        var username = Normalize(query.Username)?.ToLowerInvariant();
        var (data, total) = await loginLogRepository.GetPagedAsync(x =>
            (username == null || x.Username.ToLower().Contains(username)) &&
            (keyword == null || x.Username.ToLower().Contains(keyword) ||
             (x.FailureReason != null && x.FailureReason.ToLower().Contains(keyword)) ||
             x.IpAddress.ToLower().Contains(keyword) ||
             (x.UserAgent != null && x.UserAgent.ToLower().Contains(keyword))) &&
            (!query.IsSuccess.HasValue || x.IsSuccess == query.IsSuccess) &&
            (!query.StartTime.HasValue || x.LoginTime >= query.StartTime) && (!query.EndTime.HasValue || x.LoginTime <= query.EndTime),
            query.Current, query.Size, x => x.LoginTime, true);
        return CreatePage(mapper.Map<List<LoginLogDto>>(data), total, query.Current, query.Size);
    }

    private async Task<T> GetSettingAsync<T>(SystemSettingKey key, T defaultValue) where T : class
    {
        var setting = await systemSettingRepository.GetByConditionAsync(x => x.SettingKey == key);
        if (setting is null) return defaultValue;
        try { return JsonSerializer.Deserialize<T>(setting.SettingValue) ?? defaultValue; }
        catch (JsonException) { throw new BusinessException("系统设置数据损坏，请由管理员重新保存"); }
    }

    private async Task<T> SaveSettingAsync<T>(SystemSettingKey key, T value) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        var setting = await systemSettingRepository.GetByConditionAsync(x => x.SettingKey == key);
        if (setting is null)
        {
            setting = new SystemSetting { Id = Guid.NewGuid(), SettingKey = key, SettingValue = json, Status = Status.Enable, CreateBy = currentUserService.GetUserId(), CreateName = currentUserService.GetUserName() };
            await systemSettingRepository.AddAsync(setting);
        }
        else
        {
            setting.SettingValue = json;
            ApplyUpdateAudit(setting);
            await systemSettingRepository.UpdateAsync(setting);
        }
        await SaveWithUniqueConstraintAsync("系统设置已被其他操作更新，请重试");
        return value;
    }

    private async Task<ServicePeriod> GetServicePeriodEntityAsync(Guid id) =>
        await servicePeriodRepository.GetByIdAsync(id) ?? throw new NotFoundException("服务时段不存在");
    private async Task<Notice> GetNoticeEntityAsync(Guid id) =>
        await noticeRepository.GetByIdAsync(id) ?? throw new NotFoundException("通知公告不存在");
    private static void EnsurePage(int current, int size)
    {
        if (current < 1 || size is < 1 or > PagingConstants.MaxPageSize) throw new BusinessException("分页参数不合法");
    }
    private static void ValidateTimeRange(DateTime? start, DateTime? end)
    {
        if (start.HasValue && end.HasValue && start > end) throw new BusinessException("开始时间不能晚于结束时间");
    }
    private static ServicePeriodSnapshot ValidateServicePeriod(UpsertServicePeriodDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var name = Normalize(dto.Name) ?? throw new BusinessException("服务时段名称不能为空");
        if (name.Length > 100 || dto.EndTime <= dto.StartTime || dto.SortOrder < 0) throw new BusinessException("服务时段参数不合法");
        return new ServicePeriodSnapshot(name, dto.StartTime, dto.EndTime, dto.SortOrder, dto.IsEnabled);
    }
    private static NoticeSnapshot ValidateNotice(UpsertNoticeDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var title = Normalize(dto.Title) ?? throw new BusinessException("公告标题不能为空");
        var content = Normalize(dto.Content) ?? throw new BusinessException("公告正文不能为空");
        if (title.Length > 200 || content.Length > MaxNoticeContentLength) throw new BusinessException("公告内容长度不合法");
        if (content.Contains('<') || content.Contains('>')) throw new BusinessException("公告正文仅支持纯文本，不允许 HTML 标记");
        return new NoticeSnapshot(title, content);
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void ApplyUpdateAudit(BaseEntity entity) { entity.UpdateBy = currentUserService.GetUserId(); entity.UpdateName = currentUserService.GetUserName(); }
    private async Task SaveWithUniqueConstraintAsync(string message)
    {
        try { await unitOfWork.SaveChangesAsync(); }
        catch (Exception exception) when (exception.GetType().Name == "DbUpdateException") { throw new BusinessException(message); }
    }
    private static PagedResult<T> CreatePage<T>(List<T> records, int total, int current, int size) => new() { Records = records, Total = total, Current = current, Size = size };
    private sealed record ServicePeriodSnapshot(string Name, TimeOnly StartTime, TimeOnly EndTime, int SortOrder, bool IsEnabled);
    private sealed record NoticeSnapshot(string Title, string Content);
}
