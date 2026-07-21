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
/// 定义供应商档案和供货关系的维护用例。
/// </summary>
public interface ISupplierService : INamedCodeBaseDataService<SupplierDto, CreateSupplierDto, UpdateSupplierDto, SupplierQueryParameters>;
