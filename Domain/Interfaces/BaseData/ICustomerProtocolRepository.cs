using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     客户协议价仓储接口。
/// </summary>
public interface ICustomerProtocolRepository : INamedCodeRepository<CustomerProtocol>
{
    /// <summary>
    ///     替换协议价绑定客户关系。
    /// </summary>
    Task ReplaceCustomerRelationsAsync(Guid customerProtocolId, IEnumerable<Guid>? customerIds);
}

