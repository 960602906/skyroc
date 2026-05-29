using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;

namespace Domain.Entities.Goods;

/// <summary>
/// 商品档案实体，维护商品基础信息、默认分类、单位、供应商和仓库。
/// </summary>
public class Goods : BaseEntity
{
    /// <summary>
    /// 商品名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 商品编码。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 商品分类 ID。
    /// </summary>
    public Guid GoodsTypeId { get; set; }

    /// <summary>
    /// 基础单位 ID。
    /// </summary>
    public Guid? BaseUnitId { get; set; }

    /// <summary>
    /// 默认供应商 ID。
    /// </summary>
    public Guid? DefaultSupplierId { get; set; }

    /// <summary>
    /// 默认仓库 ID。
    /// </summary>
    public Guid? DefaultWareId { get; set; }

    /// <summary>
    /// 商品规格。
    /// </summary>
    public string? Spec { get; set; }

    /// <summary>
    /// 商品品牌。
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// 商品产地。
    /// </summary>
    public string? Origin { get; set; }

    /// <summary>
    /// 商品描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 商品税率。
    /// </summary>
    public decimal? TaxRate { get; set; }

    /// <summary>
    /// 是否上架销售。
    /// </summary>
    public bool IsOnSale { get; set; } = true;

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 商品所属分类。
    /// </summary>
    public virtual GoodsType GoodsType { get; set; } = null!;

    /// <summary>
    /// 商品基础单位。
    /// </summary>
    public virtual GoodsUnit? BaseUnit { get; set; }

    /// <summary>
    /// 默认供应商。
    /// </summary>
    public virtual Supplier? DefaultSupplier { get; set; }

    /// <summary>
    /// 默认仓库。
    /// </summary>
    public virtual Ware? DefaultWare { get; set; }

    /// <summary>
    /// 商品可用单位列表。
    /// </summary>
    public virtual ICollection<GoodsUnit> Units { get; set; } = new List<GoodsUnit>();

    /// <summary>
    /// 商品图片列表。
    /// </summary>
    public virtual ICollection<GoodsImage> Images { get; set; } = new List<GoodsImage>();

    /// <summary>
    /// 商品可供货供应商关系。
    /// </summary>
    public virtual ICollection<GoodsSupplierRelation> SupplierRelations { get; set; } = new List<GoodsSupplierRelation>();

    /// <summary>
    /// 商品关联的报价单明细。
    /// </summary>
    public virtual ICollection<QuotationGoods> QuotationGoods { get; set; } = new List<QuotationGoods>();

    /// <summary>
    /// 商品关联的客户协议价明细。
    /// </summary>
    public virtual ICollection<CustomerProtocolGoods> CustomerProtocolGoods { get; set; } = new List<CustomerProtocolGoods>();

    /// <summary>
    /// 商品关联的采购规则。
    /// </summary>
    public virtual ICollection<PurchaseRuleGoods> PurchaseRuleGoods { get; set; } = new List<PurchaseRuleGoods>();
}
