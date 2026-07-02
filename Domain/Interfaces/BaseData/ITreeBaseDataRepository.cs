using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     树形基础资料仓储接口。
/// </summary>
public interface ITreeBaseDataRepository<TEntity> : INamedCodeRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>
    ///     获取树形数据源。
    /// </summary>
    Task<List<TEntity>> GetAllTreeSourceAsync();

    /// <summary>
    ///     判断是否存在子级。
    /// </summary>
    Task<bool> HasChildrenAsync(Guid parentId);
}

