using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     报价单仓储接口。
/// </summary>
public interface IQuotationRepository : INamedCodeRepository<Quotation>
{
    /// <summary>
    ///     替换报价单绑定客户关系。
    /// </summary>
    Task ReplaceCustomerRelationsAsync(Guid quotationId, IEnumerable<Guid>? customerIds);
}

