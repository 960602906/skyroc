using Domain.Entities.AfterSales;
using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// 售后商品仓储实现，商品行随售后聚合统一事务保存。
/// </summary>
public class AfterSaleGoodsRepository(ApplicationDbContext context)
    : Repository<AfterSaleGoods>(context), IAfterSaleGoodsRepository;
