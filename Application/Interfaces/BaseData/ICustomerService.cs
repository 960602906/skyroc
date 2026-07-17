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
/// 定义客户档案和外部工商信息补充用例。
/// </summary>
public interface ICustomerService : IBaseDataService<CustomerDto, CreateCustomerDto, UpdateCustomerDto, CustomerQueryParameters>;
