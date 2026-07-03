using Application.Serialization;
using Domain.Entities.Purchases;
using System.Text.Json.Serialization;

namespace Application.DTOs.Purchases;

/// <summary>
/// 采购计划 DTO，返回计划主单、执行状态和商品明细。
/// </summary>
public class PurchasePlanDto : BaseDto
{
    /// <summary>
    /// 采购计划业务编号。
    /// </summary>
    public string PlanNo { get; set; } = string.Empty;

    /// <summary>
    /// 计划采购交期（UTC）。
    /// </summary>
    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime PlanDate { get; set; }

    /// <summary>
    /// 采购模式：供应商直供或市场自采。
    /// </summary>
    public PurchasePattern PurchasePattern { get; set; }

    /// <summary>
    /// 采购单生成进度状态。
    /// </summary>
    public PurchasePlanStatus PurchaseStatus { get; set; }

    /// <summary>
    /// 关联供应商主键，未指定时为空。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 计划生成时的供应商名称快照。
    /// </summary>
    public string? SupplierName { get; set; }

    /// <summary>
    /// 负责采购的采购员主键，未指定时为空。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 计划生成时的采购员名称快照。
    /// </summary>
    public string? PurchaserName { get; set; }

    /// <summary>
    /// 业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 采购计划商品明细集合。
    /// </summary>
    public List<PurchasePlanDetailDto> Details { get; set; } = [];
}
