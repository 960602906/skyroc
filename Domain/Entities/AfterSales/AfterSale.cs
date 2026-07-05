using Domain.Entities.Customers;
using Domain.Entities.Orders;

namespace Domain.Entities.AfterSales;

/// <summary>
/// 售后单主单，记录来源订单、客户、审核状态和结算金额快照。
/// </summary>
public class AfterSale : BaseEntity
{
    /// <summary>
    /// 售后单业务唯一编号。
    /// </summary>
    public string AfterSaleNo { get; set; } = string.Empty;

    /// <summary>
    /// 来源销售订单主键；不依赖具体订单的客户沟通类售后可为空。
    /// </summary>
    public Guid? SaleOrderId { get; set; }

    /// <summary>
    /// 建单时的销售订单编号快照；无来源订单时为空。
    /// </summary>
    public string? SaleOrderNoSnapshot { get; set; }

    /// <summary>
    /// 发起售后的客户主键。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 售后建单时的客户名称快照。
    /// </summary>
    public string CustomerNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 售后来源标识，例如后台建单或外部客户申请；由应用层维护稳定值。
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 售后单当前业务状态，初始为待提交。
    /// </summary>
    public AfterSaleStatus AfterStatus { get; set; } = AfterSaleStatus.Draft;

    /// <summary>
    /// 售后建单时的原订单金额，按系统业务币种计量。
    /// </summary>
    public decimal OrderPrice { get; set; }

    /// <summary>
    /// 售后处理后的客户结算金额，按系统业务币种计量。
    /// </summary>
    public decimal SettlementPrice { get; set; }

    /// <summary>
    /// 售后联系人的姓名快照，取货任务可据此联系客户。
    /// </summary>
    public string? ContactNameSnapshot { get; set; }

    /// <summary>
    /// 售后联系人的电话快照，取货任务可据此联系客户。
    /// </summary>
    public string? ContactPhoneSnapshot { get; set; }

    /// <summary>
    /// 售后取货地址快照；仅退款等无需取货场景可为空。
    /// </summary>
    public string? PickupAddressSnapshot { get; set; }

    /// <summary>
    /// 售后单级业务备注，对全部售后商品生效。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 来源销售订单。
    /// </summary>
    public virtual SaleOrder? SaleOrder { get; set; }

    /// <summary>
    /// 发起售后的客户档案。
    /// </summary>
    public virtual Customer Customer { get; set; } = null!;

    /// <summary>
    /// 售后商品明细集合；随未生效主单删除而删除。
    /// </summary>
    public virtual ICollection<AfterSaleGoods> Goods { get; set; } = new List<AfterSaleGoods>();

    /// <summary>
    /// 售后提交、审核、驳回和反审核轨迹集合。
    /// </summary>
    public virtual ICollection<AfterSaleAuditLog> AuditLogs { get; set; } = new List<AfterSaleAuditLog>();

    /// <summary>
    /// 售后审核通过后按退货商品生成的取货任务集合。
    /// </summary>
    public virtual ICollection<PickupTask> PickupTasks { get; set; } = new List<PickupTask>();
}
