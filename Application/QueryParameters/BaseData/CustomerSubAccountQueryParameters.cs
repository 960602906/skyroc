using System.Linq.Expressions;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Shared.Constants;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Application.QueryParameters;

/// <summary>
///     客户子账号查询参数。
/// </summary>
public class CustomerSubAccountQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     所属公司 ID。
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    ///     客户 ID。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    ///     登录账号。
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     昵称。
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    ///     启用禁用状态。
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<CustomerSubAccount, bool>> QueryBuild()
    {
        return x =>
            (!CompanyId.HasValue || x.CompanyId == CompanyId.Value) &&
            (!CustomerId.HasValue || x.CustomerId == CustomerId.Value) &&
            (string.IsNullOrWhiteSpace(Username) || x.Username.Contains(Username.Trim())) &&
            (string.IsNullOrWhiteSpace(NickName) || x.NickName.Contains(NickName.Trim())) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

