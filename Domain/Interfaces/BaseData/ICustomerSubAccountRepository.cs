using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     客户子账号仓储接口。
/// </summary>
public interface ICustomerSubAccountRepository : IRepository<CustomerSubAccount>
{
    /// <summary>
    ///     登录账号是否存在。
    /// </summary>
    Task<bool> ExistsByUsernameAsync(string username, Guid? excludeId = null);

    /// <summary>
    ///     按多个 ID 获取子账号。
    /// </summary>
    Task<List<CustomerSubAccount>> GetByIdsAsync(IEnumerable<Guid> ids);
}

