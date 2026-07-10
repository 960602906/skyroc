using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     商品档案仓储接口。
/// </summary>
public interface IGoodsRepository : INamedCodeRepository<GoodsEntity>
{
    /// <summary>
    /// 批量查询已经存在的商品名称，用于导入前一次性校验重复数据。
    /// </summary>
    /// <param name="names">待校验的非空商品名称。</param>
    /// <returns>数据库中已存在的名称集合。</returns>
    Task<IReadOnlyCollection<string>> GetExistingNamesAsync(IEnumerable<string> names);

    /// <summary>
    /// 批量查询已经存在的商品编码，用于导入前一次性校验重复数据。
    /// </summary>
    /// <param name="codes">待校验的非空商品编码。</param>
    /// <returns>数据库中已存在的编码集合。</returns>
    Task<IReadOnlyCollection<string>> GetExistingCodesAsync(IEnumerable<string> codes);

    /// <summary>
    ///     替换商品可供货供应商关系。
    /// </summary>
    Task ReplaceSupplierRelationsAsync(Guid goodsId, IEnumerable<Guid>? supplierIds, Guid? defaultSupplierId);
}
