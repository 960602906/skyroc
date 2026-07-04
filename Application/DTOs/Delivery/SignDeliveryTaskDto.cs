namespace Application.DTOs.Delivery;

/// <summary>
/// 配送任务签收请求，记录客户签收人、交付说明和本次全部出库商品验收结果。
/// </summary>
public class SignDeliveryTaskDto
{
    /// <summary>
    /// 客户侧实际签收人姓名。
    /// </summary>
    public string SignerName { get; set; } = string.Empty;

    /// <summary>
    /// 签收时记录的交付说明或客户意见。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 本配送任务全部销售出库商品行的验收结果；不得遗漏或重复。
    /// </summary>
    public List<SignDeliveryCheckDetailDto> Details { get; set; } = [];
}
