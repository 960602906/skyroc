using Domain.Entities.Customers;
using Domain.Entities.Orders;
using Domain.Entities.Pricing;
using Domain.Entities.Storage;

namespace Domain.Entities.AI;

/// <summary>
/// AI 生成的销售订单草稿，必须由所属用户在有效期内人工确认后才能创建正式订单。
/// </summary>
public class AiOrderDraft : BaseEntity
{
    /// <summary>
    /// 来源 AI 会话主键；外部 MCP 客户端独立生成草稿时可为空。
    /// </summary>
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// 草稿所属系统用户主键，也是唯一允许确认草稿的用户。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 下单客户主键。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 草稿生成时的客户名称快照。
    /// </summary>
    public string CustomerNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 草稿生成时的客户编码快照。
    /// </summary>
    public string CustomerCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 草稿采用的默认报价单主键；仅在报价价格来源下填写。
    /// </summary>
    public Guid? QuotationId { get; set; }

    /// <summary>
    /// 草稿指定的履约仓库主键。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    /// 草稿中的订单业务日期（UTC），用于解析有效价格。
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// 客户要求收货时间（UTC）。
    /// </summary>
    public DateTime? ReceiveDate { get; set; }

    /// <summary>
    /// 草稿生成时的联系人姓名快照。
    /// </summary>
    public string? ContactNameSnapshot { get; set; }

    /// <summary>
    /// 草稿生成时的联系人电话快照。
    /// </summary>
    public string? ContactPhoneSnapshot { get; set; }

    /// <summary>
    /// 草稿生成时的配送地址快照。
    /// </summary>
    public string? DeliveryAddressSnapshot { get; set; }

    /// <summary>
    /// 用户提供的订单业务备注，不保存无关内部信息。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 草稿当前业务状态。
    /// </summary>
    public AiOrderDraftStatus DraftStatus { get; set; } = AiOrderDraftStatus.PendingConfirmation;

    /// <summary>
    /// 草稿失效时间（UTC），默认自生成起 30 分钟。
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(AiPersistenceDefaults.OrderDraftLifetimeMinutes);

    /// <summary>
    /// 草稿成功创建正式订单的确认时间（UTC）；未确认时为空。
    /// </summary>
    public DateTime? ConfirmedTime { get; set; }

    /// <summary>
    /// 草稿确认后创建的正式销售订单主键；未确认或订单后续删除时为空。
    /// </summary>
    public Guid? SaleOrderId { get; set; }

    /// <summary>
    /// 草稿生成请求的幂等键，同一用户下不可重复。
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// 草稿状态更新的乐观并发版本，状态流转时必须递增。
    /// </summary>
    public long ConcurrencyVersion { get; set; } = 1;

    /// <summary>
    /// 来源 AI 会话；外部 MCP 独立草稿为空。
    /// </summary>
    public virtual AiConversation? Conversation { get; set; }

    /// <summary>
    /// 草稿所属系统用户。
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 草稿下单客户。
    /// </summary>
    public virtual Customer Customer { get; set; } = null!;

    /// <summary>
    /// 草稿采用的默认报价单。
    /// </summary>
    public virtual Quotation? Quotation { get; set; }

    /// <summary>
    /// 草稿指定的履约仓库。
    /// </summary>
    public virtual Ware? Ware { get; set; }

    /// <summary>
    /// 草稿确认后创建的正式销售订单。
    /// </summary>
    public virtual SaleOrder? SaleOrder { get; set; }

    /// <summary>
    /// 草稿商品明细集合。
    /// </summary>
    public virtual ICollection<AiOrderDraftDetail> Details { get; set; } = new List<AiOrderDraftDetail>();
}
