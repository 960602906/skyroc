using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     带名称和编码的基础资料仓储接口。
/// </summary>
public interface INamedCodeRepository<TEntity> : IRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>
    ///     按编码判断是否存在，可排除当前记录。
    /// </summary>
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null);

    /// <summary>
    ///     按名称判断是否存在，可排除当前记录。
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);

    /// <summary>
    ///     按多个 ID 获取基础资料。
    /// </summary>
    Task<List<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids);
}

/// <summary>
///     树形基础资料仓储接口。
/// </summary>
public interface ITreeBaseDataRepository<TEntity> : INamedCodeRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>
    ///     获取树形数据源。
    /// </summary>
    Task<List<TEntity>> GetAllTreeSourceAsync();

    /// <summary>
    ///     判断是否存在子级。
    /// </summary>
    Task<bool> HasChildrenAsync(Guid parentId);
}

/// <summary>
///     商品分类仓储接口。
/// </summary>
public interface IGoodsTypeRepository : ITreeBaseDataRepository<GoodsType>
{
}

/// <summary>
///     商品档案仓储接口。
/// </summary>
public interface IGoodsRepository : INamedCodeRepository<GoodsEntity>
{
    /// <summary>
    ///     替换商品可供货供应商关系。
    /// </summary>
    Task ReplaceSupplierRelationsAsync(Guid goodsId, IEnumerable<Guid>? supplierIds, Guid? defaultSupplierId);
}

/// <summary>
///     商品单位仓储接口。
/// </summary>
public interface IGoodsUnitRepository : IRepository<GoodsUnit>
{
    /// <summary>
    ///     查询商品单位。
    /// </summary>
    Task<List<GoodsUnit>> GetByGoodsIdAsync(Guid goodsId);

    /// <summary>
    ///     同一商品下单位名称是否存在。
    /// </summary>
    Task<bool> ExistsByGoodsAndNameAsync(Guid goodsId, string name, Guid? excludeId = null);

    /// <summary>
    ///     将指定单位设置为商品基础单位。
    /// </summary>
    Task SetBaseUnitAsync(Guid goodsId, Guid unitId);
}

/// <summary>
///     公司仓储接口。
/// </summary>
public interface ICompanyRepository : INamedCodeRepository<Company>
{
}

/// <summary>
///     客户仓储接口。
/// </summary>
public interface ICustomerRepository : INamedCodeRepository<Customer>
{
    /// <summary>
    ///     替换客户标签关系。
    /// </summary>
    Task ReplaceTagRelationsAsync(Guid customerId, IEnumerable<Guid>? tagIds);
}

/// <summary>
///     客户标签仓储接口。
/// </summary>
public interface ICustomerTagRepository : ITreeBaseDataRepository<CustomerTag>
{
    /// <summary>
    ///     判断标签是否已被客户使用。
    /// </summary>
    Task<bool> HasCustomersAsync(Guid tagId);
}

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

/// <summary>
///     供应商仓储接口。
/// </summary>
public interface ISupplierRepository : INamedCodeRepository<Supplier>
{
}

/// <summary>
///     采购员仓储接口。
/// </summary>
public interface IPurchaserRepository : INamedCodeRepository<Purchaser>
{
}

/// <summary>
///     仓库仓储接口。
/// </summary>
public interface IWareRepository : INamedCodeRepository<Ware>
{
}

/// <summary>
///     报价单仓储接口。
/// </summary>
public interface IQuotationRepository : INamedCodeRepository<Quotation>
{
    /// <summary>
    ///     替换报价单绑定客户关系。
    /// </summary>
    Task ReplaceCustomerRelationsAsync(Guid quotationId, IEnumerable<Guid>? customerIds);
}

/// <summary>
///     报价商品仓储接口。
/// </summary>
public interface IQuotationGoodsRepository : IRepository<QuotationGoods>
{
    /// <summary>
    ///     判断报价商品明细是否重复。
    /// </summary>
    Task<bool> ExistsDetailAsync(Guid quotationId, Guid goodsId, Guid goodsUnitId, Guid? excludeId = null);

    /// <summary>
    ///     按多个 ID 获取报价商品明细。
    /// </summary>
    Task<List<QuotationGoods>> GetByIdsAsync(IEnumerable<Guid> ids);
}

/// <summary>
///     客户协议价仓储接口。
/// </summary>
public interface ICustomerProtocolRepository : INamedCodeRepository<CustomerProtocol>
{
    /// <summary>
    ///     替换协议价绑定客户关系。
    /// </summary>
    Task ReplaceCustomerRelationsAsync(Guid customerProtocolId, IEnumerable<Guid>? customerIds);
}

/// <summary>
///     客户协议价商品仓储接口。
/// </summary>
public interface ICustomerProtocolGoodsRepository : IRepository<CustomerProtocolGoods>
{
    /// <summary>
    ///     判断协议价商品明细是否重复。
    /// </summary>
    Task<bool> ExistsDetailAsync(Guid customerProtocolId, Guid goodsId, Guid goodsUnitId, Guid? excludeId = null);

    /// <summary>
    ///     按多个 ID 获取协议价商品明细。
    /// </summary>
    Task<List<CustomerProtocolGoods>> GetByIdsAsync(IEnumerable<Guid> ids);
}

/// <summary>
///     采购规则仓储接口。
/// </summary>
public interface IPurchaseRuleRepository : INamedCodeRepository<PurchaseRule>
{
    /// <summary>
    ///     替换采购规则适用商品关系。
    /// </summary>
    Task ReplaceGoodsRelationsAsync(Guid purchaseRuleId, IEnumerable<Guid>? goodsIds);

    /// <summary>
    ///     替换采购规则适用客户关系。
    /// </summary>
    Task ReplaceCustomerRelationsAsync(Guid purchaseRuleId, IEnumerable<Guid>? customerIds);
}
