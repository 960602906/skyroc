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
///     采购员仓储。
/// </summary>
public class PurchaserRepository(ApplicationDbContext context)
    : NamedCodeRepository<Purchaser>(context), IPurchaserRepository
{
    /// <summary>
    ///     按主键查询采购员，并加载关联系统用户与部门，供详情回填名称快照。
    /// </summary>
    public override async Task<Purchaser?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.User)
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}

