using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 定义采购员档案的查询和维护用例。
/// </summary>
public interface IPurchaserService : IBaseDataService<PurchaserDto, CreatePurchaserDto, UpdatePurchaserDto, PurchaserQueryParameters>;
