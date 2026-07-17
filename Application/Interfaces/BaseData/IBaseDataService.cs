using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
///     基础资料通用应用服务接口。
/// </summary>
public interface IBaseDataService<TDto, in TCreateDto, in TUpdateDto, in TQuery>
    where TUpdateDto : IHasId
{
    /// <summary>
    ///     分页查询。
    /// </summary>
    Task<PagedResult<TDto>> GetPagedAsync(TQuery parameters);

    /// <summary>
    ///     查询全部。
    /// </summary>
    Task<List<TDto>> GetAllAsync();

    /// <summary>
    ///     根据 ID 查询。
    /// </summary>
    Task<TDto> GetByIdAsync(Guid id);

    /// <summary>
    ///     创建。
    /// </summary>
    Task<TDto> CreateAsync(TCreateDto dto);

    /// <summary>
    ///     更新。
    /// </summary>
    Task<TDto> UpdateAsync(Guid id, TUpdateDto dto);

    /// <summary>
    ///     删除。
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    ///     批量删除。
    /// </summary>
    Task<bool> BatchDeleteAsync(List<Guid> ids);

    /// <summary>
    ///     启用或禁用。
    /// </summary>
    Task<TDto> ToggleStatusAsync(Guid id, Status status);
}

