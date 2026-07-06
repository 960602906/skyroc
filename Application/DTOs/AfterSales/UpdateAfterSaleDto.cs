namespace Application.DTOs.AfterSales;

/// <summary>
/// 更新待提交售后单请求；来源订单、客户和来源标识在创建后不可变更。
/// </summary>
public class UpdateAfterSaleDto
{
    /// <summary>待更新售后单主键。</summary>
    public Guid Id { get; set; }

    /// <summary>售后联系人姓名快照，最长 100 字符。</summary>
    public string? ContactName { get; set; }

    /// <summary>售后联系人电话快照，最长 30 字符。</summary>
    public string? ContactPhone { get; set; }

    /// <summary>需要回收实物时使用的取货地址，最长 500 字符。</summary>
    public string? PickupAddress { get; set; }

    /// <summary>对全部商品行生效的售后备注，最长 500 字符。</summary>
    public string? Remark { get; set; }

    /// <summary>替换后的完整售后商品行集合，至少一条。</summary>
    public List<CreateAfterSaleGoodsDto> Goods { get; set; } = [];
}
