using Domain.Entities.AfterSales;

namespace Domain.Interfaces;

/// <summary>
/// 售后商品仓储，用于在待提交售后编辑时原子替换商品行。
/// </summary>
public interface IAfterSaleGoodsRepository : IRepository<AfterSaleGoods>;
