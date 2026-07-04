using Domain.Entities.Customers;

namespace Domain.Entities.Delivery;

/// <summary>
/// 客户路线关系实体，记录客户被分配到的配送路线及在该路线内的配送顺序。
/// 同一客户在同一路线内只允许存在一条关系。
/// </summary>
public class CustomerRoute : BaseEntity
{
    /// <summary>
    /// 关联配送路线主键。
    /// </summary>
    public Guid RouteId { get; set; }

    /// <summary>
    /// 关联客户主键。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 客户在该路线内的配送排序值，数值越小越靠前。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 关联配送路线。
    /// </summary>
    public virtual DeliveryRoute? Route { get; set; }

    /// <summary>
    /// 关联客户。
    /// </summary>
    public virtual Customer? Customer { get; set; }
}
