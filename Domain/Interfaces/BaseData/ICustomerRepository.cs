using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     客户仓储接口。
/// </summary>
public interface ICustomerRepository : INamedCodeRepository<Customer>
{
    /// <summary>
    ///     替换客户标签关系。
    /// </summary>
    Task ReplaceTagRelationsAsync(Guid customerId, IEnumerable<Guid>? tagIds);
}

