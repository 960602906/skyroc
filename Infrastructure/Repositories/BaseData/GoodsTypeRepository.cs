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
///     商品分类仓储。
/// </summary>
public class GoodsTypeRepository(ApplicationDbContext context)
    : TreeBaseDataRepository<GoodsType>(context), IGoodsTypeRepository;

