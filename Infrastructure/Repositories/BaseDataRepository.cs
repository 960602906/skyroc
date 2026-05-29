using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Infrastructure.Repositories;

/// <summary>
///     带名称和编码的基础资料仓储基类。
/// </summary>
public abstract class NamedCodeRepository<TEntity>(ApplicationDbContext context) : Repository<TEntity>(context),
    INamedCodeRepository<TEntity>
    where TEntity : BaseEntity
{
    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null)
    {
        var normalizedCode = code.Trim();
        var query = DbSet.Where(x => EF.Property<string>(x, "Code") == normalizedCode);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        var normalizedName = name.Trim();
        var query = DbSet.Where(x => EF.Property<string>(x, "Name") == normalizedName);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<List<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        return idList.Count == 0
            ? []
            : await DbSet.Where(x => idList.Contains(x.Id)).ToListAsync();
    }
}

/// <summary>
///     树形基础资料仓储基类。
/// </summary>
public abstract class TreeBaseDataRepository<TEntity>(ApplicationDbContext context)
    : NamedCodeRepository<TEntity>(context), ITreeBaseDataRepository<TEntity>
    where TEntity : BaseEntity
{
    public virtual async Task<List<TEntity>> GetAllTreeSourceAsync()
    {
        return await DbSet
            .AsNoTracking()
            .OrderBy(x => EF.Property<int>(x, "Sort"))
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<bool> HasChildrenAsync(Guid parentId)
    {
        return await DbSet.AnyAsync(x => EF.Property<Guid?>(x, "ParentId") == parentId);
    }
}

/// <summary>
///     商品分类仓储。
/// </summary>
public class GoodsTypeRepository(ApplicationDbContext context)
    : TreeBaseDataRepository<GoodsType>(context), IGoodsTypeRepository;

/// <summary>
///     商品档案仓储。
/// </summary>
public class GoodsRepository(ApplicationDbContext context)
    : NamedCodeRepository<GoodsEntity>(context), IGoodsRepository
{
    private readonly ApplicationDbContext _context = context;

    public override async Task<GoodsEntity?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.GoodsType)
            .Include(x => x.BaseUnit)
            .Include(x => x.DefaultSupplier)
            .Include(x => x.DefaultWare)
            .Include(x => x.Units)
            .Include(x => x.Images)
            .Include(x => x.SupplierRelations)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task ReplaceSupplierRelationsAsync(Guid goodsId, IEnumerable<Guid>? supplierIds, Guid? defaultSupplierId)
    {
        var relations = await _context.Set<GoodsSupplierRelation>()
            .Where(x => x.GoodsId == goodsId)
            .ToListAsync();
        _context.Set<GoodsSupplierRelation>().RemoveRange(relations);

        var supplierIdList = supplierIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList() ?? [];

        if (defaultSupplierId.HasValue && defaultSupplierId.Value != Guid.Empty && !supplierIdList.Contains(defaultSupplierId.Value))
        {
            supplierIdList.Add(defaultSupplierId.Value);
        }

        var newRelations = supplierIdList.Select(supplierId => new GoodsSupplierRelation
        {
            GoodsId = goodsId,
            SupplierId = supplierId,
            IsDefault = defaultSupplierId.HasValue && supplierId == defaultSupplierId.Value
        });
        await _context.Set<GoodsSupplierRelation>().AddRangeAsync(newRelations);
    }
}

/// <summary>
///     商品单位仓储。
/// </summary>
public class GoodsUnitRepository(ApplicationDbContext context)
    : Repository<GoodsUnit>(context), IGoodsUnitRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<List<GoodsUnit>> GetByGoodsIdAsync(Guid goodsId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.GoodsId == goodsId)
            .OrderBy(x => x.Sort)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<bool> ExistsByGoodsAndNameAsync(Guid goodsId, string name, Guid? excludeId = null)
    {
        var normalizedName = name.Trim();
        var query = DbSet.Where(x => x.GoodsId == goodsId && x.Name == normalizedName);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task SetBaseUnitAsync(Guid goodsId, Guid unitId)
    {
        var units = await DbSet.Where(x => x.GoodsId == goodsId).ToListAsync();
        foreach (var unit in units)
        {
            unit.IsBaseUnit = unit.Id == unitId;
        }

        var goods = await _context.Set<GoodsEntity>().FirstOrDefaultAsync(x => x.Id == goodsId);
        if (goods is not null)
        {
            goods.BaseUnitId = unitId;
        }
    }
}

/// <summary>
///     公司仓储。
/// </summary>
public class CompanyRepository(ApplicationDbContext context)
    : NamedCodeRepository<Company>(context), ICompanyRepository;

/// <summary>
///     客户仓储。
/// </summary>
public class CustomerRepository(ApplicationDbContext context)
    : NamedCodeRepository<Customer>(context), ICustomerRepository
{
    private readonly ApplicationDbContext _context = context;

    public override async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Company)
            .Include(x => x.Quotation)
            .Include(x => x.DefaultWare)
            .Include(x => x.TagRelations)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task ReplaceTagRelationsAsync(Guid customerId, IEnumerable<Guid>? tagIds)
    {
        var relations = await _context.Set<CustomerTagRelation>()
            .Where(x => x.CustomerId == customerId)
            .ToListAsync();
        _context.Set<CustomerTagRelation>().RemoveRange(relations);

        var newRelations = tagIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select(tagId => new CustomerTagRelation
            {
                CustomerId = customerId,
                CustomerTagId = tagId
            }) ?? [];

        await _context.Set<CustomerTagRelation>().AddRangeAsync(newRelations);
    }
}

/// <summary>
///     客户标签仓储。
/// </summary>
public class CustomerTagRepository(ApplicationDbContext context)
    : TreeBaseDataRepository<CustomerTag>(context), ICustomerTagRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<bool> HasCustomersAsync(Guid tagId)
    {
        return await _context.Set<CustomerTagRelation>().AnyAsync(x => x.CustomerTagId == tagId);
    }
}

/// <summary>
///     客户子账号仓储。
/// </summary>
public class CustomerSubAccountRepository(ApplicationDbContext context)
    : Repository<CustomerSubAccount>(context), ICustomerSubAccountRepository
{
    public override async Task<CustomerSubAccount?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Company)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> ExistsByUsernameAsync(string username, Guid? excludeId = null)
    {
        var normalizedUsername = username.Trim();
        var query = DbSet.Where(x => x.Username == normalizedUsername);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<List<CustomerSubAccount>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        return idList.Count == 0
            ? []
            : await DbSet.Where(x => idList.Contains(x.Id)).ToListAsync();
    }
}

/// <summary>
///     供应商仓储。
/// </summary>
public class SupplierRepository(ApplicationDbContext context)
    : NamedCodeRepository<Supplier>(context), ISupplierRepository;

/// <summary>
///     采购员仓储。
/// </summary>
public class PurchaserRepository(ApplicationDbContext context)
    : NamedCodeRepository<Purchaser>(context), IPurchaserRepository;

/// <summary>
///     仓库仓储。
/// </summary>
public class WareRepository(ApplicationDbContext context)
    : NamedCodeRepository<Ware>(context), IWareRepository;

/// <summary>
///     报价单仓储。
/// </summary>
public class QuotationRepository(ApplicationDbContext context)
    : NamedCodeRepository<Quotation>(context), IQuotationRepository
{
    private readonly ApplicationDbContext _context = context;

    public override async Task<Quotation?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Goods)
                .ThenInclude(x => x.Goods)
            .Include(x => x.Goods)
                .ThenInclude(x => x.GoodsUnit)
            .Include(x => x.CustomerQuotations)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task ReplaceCustomerRelationsAsync(Guid quotationId, IEnumerable<Guid>? customerIds)
    {
        var relations = await _context.Set<CustomerQuotation>()
            .Where(x => x.QuotationId == quotationId)
            .ToListAsync();
        _context.Set<CustomerQuotation>().RemoveRange(relations);

        var newRelations = customerIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select(customerId => new CustomerQuotation
            {
                QuotationId = quotationId,
                CustomerId = customerId,
                IsDefault = false
            }) ?? [];

        await _context.Set<CustomerQuotation>().AddRangeAsync(newRelations);
    }
}

/// <summary>
///     报价商品仓储。
/// </summary>
public class QuotationGoodsRepository(ApplicationDbContext context)
    : Repository<QuotationGoods>(context), IQuotationGoodsRepository
{
    public override async Task<QuotationGoods?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Goods)
            .Include(x => x.GoodsUnit)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> ExistsDetailAsync(Guid quotationId, Guid goodsId, Guid goodsUnitId, Guid? excludeId = null)
    {
        var query = DbSet.Where(x => x.QuotationId == quotationId && x.GoodsId == goodsId && x.GoodsUnitId == goodsUnitId);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<List<QuotationGoods>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        return idList.Count == 0
            ? []
            : await DbSet.Where(x => idList.Contains(x.Id)).ToListAsync();
    }
}

/// <summary>
///     客户协议价仓储。
/// </summary>
public class CustomerProtocolRepository(ApplicationDbContext context)
    : NamedCodeRepository<CustomerProtocol>(context), ICustomerProtocolRepository
{
    private readonly ApplicationDbContext _context = context;

    public override async Task<CustomerProtocol?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Quotation)
            .Include(x => x.Goods)
                .ThenInclude(x => x.Goods)
            .Include(x => x.Goods)
                .ThenInclude(x => x.GoodsUnit)
            .Include(x => x.Customers)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task ReplaceCustomerRelationsAsync(Guid customerProtocolId, IEnumerable<Guid>? customerIds)
    {
        var relations = await _context.Set<CustomerProtocolCustomer>()
            .Where(x => x.CustomerProtocolId == customerProtocolId)
            .ToListAsync();
        _context.Set<CustomerProtocolCustomer>().RemoveRange(relations);

        var newRelations = customerIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select(customerId => new CustomerProtocolCustomer
            {
                CustomerProtocolId = customerProtocolId,
                CustomerId = customerId
            }) ?? [];

        await _context.Set<CustomerProtocolCustomer>().AddRangeAsync(newRelations);
    }
}

/// <summary>
///     客户协议价商品仓储。
/// </summary>
public class CustomerProtocolGoodsRepository(ApplicationDbContext context)
    : Repository<CustomerProtocolGoods>(context), ICustomerProtocolGoodsRepository
{
    public override async Task<CustomerProtocolGoods?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Goods)
            .Include(x => x.GoodsUnit)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> ExistsDetailAsync(Guid customerProtocolId, Guid goodsId, Guid goodsUnitId, Guid? excludeId = null)
    {
        var query = DbSet.Where(x =>
            x.CustomerProtocolId == customerProtocolId &&
            x.GoodsId == goodsId &&
            x.GoodsUnitId == goodsUnitId);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<List<CustomerProtocolGoods>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        return idList.Count == 0
            ? []
            : await DbSet.Where(x => idList.Contains(x.Id)).ToListAsync();
    }
}

/// <summary>
///     采购规则仓储。
/// </summary>
public class PurchaseRuleRepository(ApplicationDbContext context)
    : NamedCodeRepository<PurchaseRule>(context), IPurchaseRuleRepository
{
    private readonly ApplicationDbContext _context = context;

    public override async Task<PurchaseRule?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Supplier)
            .Include(x => x.Purchaser)
            .Include(x => x.Ware)
            .Include(x => x.GoodsType)
            .Include(x => x.Goods)
            .Include(x => x.Customers)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task ReplaceGoodsRelationsAsync(Guid purchaseRuleId, IEnumerable<Guid>? goodsIds)
    {
        var relations = await _context.Set<PurchaseRuleGoods>()
            .Where(x => x.PurchaseRuleId == purchaseRuleId)
            .ToListAsync();
        _context.Set<PurchaseRuleGoods>().RemoveRange(relations);

        var newRelations = goodsIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select(goodsId => new PurchaseRuleGoods
            {
                PurchaseRuleId = purchaseRuleId,
                GoodsId = goodsId
            }) ?? [];

        await _context.Set<PurchaseRuleGoods>().AddRangeAsync(newRelations);
    }

    public async Task ReplaceCustomerRelationsAsync(Guid purchaseRuleId, IEnumerable<Guid>? customerIds)
    {
        var relations = await _context.Set<PurchaseRuleCustomer>()
            .Where(x => x.PurchaseRuleId == purchaseRuleId)
            .ToListAsync();
        _context.Set<PurchaseRuleCustomer>().RemoveRange(relations);

        var newRelations = customerIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select(customerId => new PurchaseRuleCustomer
            {
                PurchaseRuleId = purchaseRuleId,
                CustomerId = customerId
            }) ?? [];

        await _context.Set<PurchaseRuleCustomer>().AddRangeAsync(newRelations);
    }
}
