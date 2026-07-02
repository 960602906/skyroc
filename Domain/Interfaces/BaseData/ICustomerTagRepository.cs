using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     客户标签仓储接口。
/// </summary>
public interface ICustomerTagRepository : ITreeBaseDataRepository<CustomerTag>
{
    /// <summary>
    ///     判断标签是否已被客户使用。
    /// </summary>
    Task<bool> HasCustomersAsync(Guid tagId);
}

