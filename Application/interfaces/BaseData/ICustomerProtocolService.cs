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
/// 定义客户协议价及适用客户关系的维护用例。
/// </summary>
public interface ICustomerProtocolService : INamedCodeBaseDataService<CustomerProtocolDto, CreateCustomerProtocolDto, UpdateCustomerProtocolDto, CustomerProtocolQueryParameters>;
