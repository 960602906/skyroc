namespace Application.DTOs.AfterSales;

/// <summary>
/// 创建售后单请求；新单保存为待提交，不会自动进入审核。
/// </summary>
public class CreateAfterSaleDto
{
    /// <summary>来源销售订单主键；客户独立申请时可为空。</summary>
    public Guid? SaleOrderId { get; set; }

    /// <summary>发起售后的客户主键；有关联订单时可省略，填写时必须与订单客户一致。</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>售后来源稳定标识，例如“后台建单”，最长 50 字符。</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>售后联系人姓名快照，最长 100 字符。</summary>
    public string? ContactName { get; set; }

    /// <summary>售后联系人电话快照，最长 30 字符。</summary>
    public string? ContactPhone { get; set; }

    /// <summary>需要回收实物时使用的取货地址，最长 500 字符。</summary>
    public string? PickupAddress { get; set; }

    /// <summary>对全部商品行生效的售后备注，最长 500 字符。</summary>
    public string? Remark { get; set; }

    /// <summary>至少一条售后商品行。</summary>
    public List<CreateAfterSaleGoodsDto> Goods { get; set; } = [];
}
