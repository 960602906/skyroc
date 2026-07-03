using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.interfaces;

/// <summary>
/// 定义客户子账号及授权关系的维护用例。
/// </summary>
public interface ICustomerSubAccountService : IBaseDataService<CustomerSubAccountDto, CreateCustomerSubAccountDto, UpdateCustomerSubAccountDto, CustomerSubAccountQueryParameters>;
