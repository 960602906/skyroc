using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.interfaces;

/// <summary>
/// 定义商品单位和单位换算的维护用例。
/// </summary>
public interface IGoodsUnitService : IBaseDataService<GoodsUnitDto, CreateGoodsUnitDto, UpdateGoodsUnitDto, GoodsUnitQueryParameters>
{
    /// <summary>
    ///     查询商品单位列表。
    /// </summary>
    Task<List<GoodsUnitDto>> GetByGoodsIdAsync(Guid goodsId);
}
