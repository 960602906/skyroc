using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     带名称和编码的基础资料仓储接口。
/// </summary>
public interface INamedCodeRepository<TEntity> : IRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>
    ///     按编码判断是否存在，可排除当前记录。
    /// </summary>
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null);

    /// <summary>
    ///     按名称判断是否存在，可排除当前记录。
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);

    /// <summary>
    ///     按多个 ID 获取基础资料。
    /// </summary>
    Task<List<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids);
}

