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
/// 定义采购规则及适用范围的维护用例。
/// </summary>
public interface IPurchaseRuleService : INamedCodeBaseDataService<PurchaseRuleDto, CreatePurchaseRuleDto, UpdatePurchaseRuleDto, PurchaseRuleQueryParameters>;
