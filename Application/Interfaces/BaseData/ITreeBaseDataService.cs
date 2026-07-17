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
///     树形基础资料通用应用服务接口。
/// </summary>
public interface ITreeBaseDataService<TDto, in TCreateDto, in TUpdateDto, in TQuery>
    : IBaseDataService<TDto, TCreateDto, TUpdateDto, TQuery>
    where TUpdateDto : IHasId
{
    /// <summary>
    ///     获取树形结构。
    /// </summary>
    Task<List<TDto>> GetTreeAsync();
}

