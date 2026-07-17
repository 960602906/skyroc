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
/// 定义商品档案、上下架和供应商关系的维护用例。
/// </summary>
public interface IGoodsService : IBaseDataService<GoodsDto, CreateGoodsDto, UpdateGoodsDto, GoodsQueryParameters>
{
    /// <summary>
    ///     修改商品上下架状态。
    /// </summary>
    Task<GoodsDto> ToggleSaleStatusAsync(Guid id, bool isOnSale);
}
